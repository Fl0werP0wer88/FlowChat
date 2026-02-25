using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.Messaging.Runtime.Kafka.GenericConsumer;

public sealed class GenericKafkaConsumerBackgroundService : BackgroundService
{
    public const string RetryCountHeader = "x-retry-count";
    public const string OriginalTopicHeader = "x-original-topic";
    public const string LastErrorHeader = "x-last-error";
    public const string RetryAtUtcHeader = "x-retry-at-utc";

    private static readonly TimeSpan ConsumeTimeout = TimeSpan.FromMilliseconds(250);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyDictionary<string, ITopicSubscription> _subscriptionsByTopic;
    private readonly KafkaConsumerRuntimeOptions _options;
    private readonly ILogger<GenericKafkaConsumerBackgroundService> _logger;

    private sealed record DeferredRetryMessage(
        ConsumeResult<string, string> ConsumeResult,
        DateTime RetryAtUtc);

    public GenericKafkaConsumerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IEnumerable<ITopicSubscription> subscriptions,
        IOptions<KafkaConsumerRuntimeOptions> options,
        ILogger<GenericKafkaConsumerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        var map = new Dictionary<string, ITopicSubscription>(StringComparer.Ordinal);
        foreach (var subscription in subscriptions)
        {
            foreach (var topic in subscription.TopicsToSubscribe.Where(topic => !string.IsNullOrWhiteSpace(topic)))
            {
                if (!map.TryAdd(topic, subscription))
                {
                    throw new InvalidOperationException($"Duplicate topic subscription detected: '{topic}'.");
                }
            }
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException("No Kafka topic subscriptions configured.");
        }

        _subscriptionsByTopic = map;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ValidateOptions(_options);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = ParseAutoOffsetReset(_options.AutoOffsetReset),
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var topics = _subscriptionsByTopic.Keys.ToArray();
        consumer.Subscribe(topics);

        _logger.LogInformation(
            "Generic Kafka consumer started. Topics: {Topics}, GroupId: {GroupId}",
            string.Join(",", topics),
            _options.GroupId);

        var deferredRetryMessages = new Dictionary<TopicPartition, DeferredRetryMessage>();

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = null;

            try
            {
                await ProcessReadyDeferredRetriesAsync(
                    consumer,
                    producer,
                    deferredRetryMessages,
                    stoppingToken);

                RemoveDeferredMessagesForRevokedPartitions(consumer, deferredRetryMessages);

                consumeResult = consumer.Consume(ConsumeTimeout);
                if (consumeResult is null || consumeResult.Message?.Value is null)
                {
                    continue;
                }

                if (!_subscriptionsByTopic.TryGetValue(consumeResult.Topic, out var subscription))
                {
                    _logger.LogWarning(
                        "No subscription found for topic {Topic}. Message with key {Key} will be skipped.",
                        consumeResult.Topic,
                        consumeResult.Message.Key);
                    consumer.Commit(consumeResult);
                    continue;
                }

                if (ShouldDeferRetryMessage(consumeResult, subscription, out var retryAtUtc))
                {
                    DeferRetryMessage(consumer, deferredRetryMessages, consumeResult, retryAtUtc);
                    continue;
                }

                var shouldCommit = await ProcessMessageAsync(consumeResult, producer, stoppingToken);
                if (shouldCommit)
                {
                    consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (KafkaException ex)
            {
                _logger.LogError(ex, "Kafka processing error for topic {Topic}.", consumeResult?.Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while consuming Kafka events.");
            }
        }

        if (deferredRetryMessages.Count > 0)
        {
            consumer.Resume(deferredRetryMessages.Keys);
            deferredRetryMessages.Clear();
        }

        _logger.LogInformation("Kafka consumer stopping.");
        consumer.Close();
    }

    private bool ShouldDeferRetryMessage(
        ConsumeResult<string, string> consumeResult,
        ITopicSubscription subscription,
        out DateTime retryAtUtc)
    {
        retryAtUtc = default;

        if (string.IsNullOrWhiteSpace(subscription.RetryTopic) ||
            !string.Equals(consumeResult.Topic, subscription.RetryTopic, StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryGetRetryAtUtc(consumeResult.Message.Headers, out retryAtUtc))
        {
            return false;
        }

        return retryAtUtc > DateTime.UtcNow;
    }

    private void DeferRetryMessage(
        IConsumer<string, string> consumer,
        Dictionary<TopicPartition, DeferredRetryMessage> deferredRetryMessages,
        ConsumeResult<string, string> consumeResult,
        DateTime retryAtUtc)
    {
        var topicPartition = consumeResult.TopicPartition;

        consumer.Pause([topicPartition]);

        deferredRetryMessages[topicPartition] = new DeferredRetryMessage(consumeResult, retryAtUtc);

        _logger.LogInformation(
            "Deferred retry message for key {Key} on {TopicPartition} until {RetryAtUtc}.",
            consumeResult.Message.Key,
            topicPartition,
            retryAtUtc);
    }

    private async Task ProcessReadyDeferredRetriesAsync(
        IConsumer<string, string> consumer,
        IProducer<string, string> producer,
        Dictionary<TopicPartition, DeferredRetryMessage> deferredRetryMessages,
        CancellationToken cancellationToken)
    {
        if (deferredRetryMessages.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var readyTopicPartitions = deferredRetryMessages
            .Where(entry => entry.Value.RetryAtUtc <= now)
            .Select(entry => entry.Key)
            .ToArray();

        foreach (var topicPartition in readyTopicPartitions)
        {
            if (!deferredRetryMessages.TryGetValue(topicPartition, out var deferredMessage))
            {
                continue;
            }

            try
            {
                var shouldCommit = await ProcessMessageAsync(
                    deferredMessage.ConsumeResult,
                    producer,
                    cancellationToken);

                if (shouldCommit)
                {
                    consumer.Commit(deferredMessage.ConsumeResult);
                }
            }
            finally
            {
                deferredRetryMessages.Remove(topicPartition);
                consumer.Resume([topicPartition]);
            }
        }
    }

    private void RemoveDeferredMessagesForRevokedPartitions(
        IConsumer<string, string> consumer,
        Dictionary<TopicPartition, DeferredRetryMessage> deferredRetryMessages)
    {
        if (deferredRetryMessages.Count == 0)
        {
            return;
        }

        var assignedPartitions = consumer.Assignment.ToHashSet();
        var revokedPartitions = deferredRetryMessages.Keys
            .Where(topicPartition => !assignedPartitions.Contains(topicPartition))
            .ToArray();

        foreach (var revokedPartition in revokedPartitions)
        {
            if (!deferredRetryMessages.Remove(revokedPartition, out var deferredMessage))
            {
                continue;
            }

            _logger.LogWarning(
                "Dropped deferred retry message for revoked partition {TopicPartition}. Message key: {Key}",
                revokedPartition,
                deferredMessage.ConsumeResult.Message.Key);
        }
    }

    private async Task<bool> ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        IProducer<string, string> producer,
        CancellationToken cancellationToken)
    {
        if (!_subscriptionsByTopic.TryGetValue(consumeResult.Topic, out var subscription))
        {
            _logger.LogWarning(
                "No subscription found for topic {Topic}. Message with key {Key} will be skipped.",
                consumeResult.Topic,
                consumeResult.Message.Key);
            return true;
        }

        MessageHandlingResult handlingResult;

        using var scope = _scopeFactory.CreateScope();

        try
        {
            handlingResult = await subscription.HandleAsync(consumeResult, scope.ServiceProvider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            handlingResult = MessageHandlingResult.Retry(ex.Message);
            _logger.LogWarning(
                ex,
                "Subscription handler failed for topic {Topic} and key {Key}.",
                consumeResult.Topic,
                consumeResult.Message.Key);
        }

        return handlingResult.Action switch
        {
            MessageHandlingAction.Success => true,
            MessageHandlingAction.Skip => true,
            MessageHandlingAction.Retry => await RouteToRetryOrDeadLetterAsync(
                consumeResult,
                producer,
                subscription,
                handlingResult.Error,
                cancellationToken),
            MessageHandlingAction.DeadLetter => await RouteToDeadLetterAsync(
                consumeResult,
                producer,
                subscription,
                handlingResult.Error,
                cancellationToken),
            _ => true
        };
    }

    private async Task<bool> RouteToRetryOrDeadLetterAsync(
        ConsumeResult<string, string> consumeResult,
        IProducer<string, string> producer,
        ITopicSubscription subscription,
        string? error,
        CancellationToken cancellationToken)
    {
        var currentRetryCount = GetRetryCount(consumeResult.Message.Headers);
        var nextRetryCount = currentRetryCount + 1;

        if (nextRetryCount <= subscription.MaxRetryCount && !string.IsNullOrWhiteSpace(subscription.RetryTopic))
        {
            var retryDelay = ComputeRetryDelay(nextRetryCount);
            var retryAtUtc = DateTime.UtcNow.Add(retryDelay);

            var retryMessage = new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = consumeResult.Message.Value,
                Headers = BuildHeaders(consumeResult, nextRetryCount, error, retryAtUtc)
            };

            await producer.ProduceAsync(subscription.RetryTopic, retryMessage, cancellationToken);

            _logger.LogWarning(
                "Routed message with key {Key} from {Topic} to retry topic {RetryTopic}. Retry count: {RetryCount}. Retry at: {RetryAtUtc}",
                consumeResult.Message.Key,
                consumeResult.Topic,
                subscription.RetryTopic,
                nextRetryCount,
                retryAtUtc);

            return true;
        }

        return await RouteToDeadLetterAsync(consumeResult, producer, subscription, error, cancellationToken);
    }

    private async Task<bool> RouteToDeadLetterAsync(
        ConsumeResult<string, string> consumeResult,
        IProducer<string, string> producer,
        ITopicSubscription subscription,
        string? error,
        CancellationToken cancellationToken)
    {
        var currentRetryCount = GetRetryCount(consumeResult.Message.Headers);

        if (!string.IsNullOrWhiteSpace(subscription.DeadLetterTopic))
        {
            var deadLetterMessage = new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = consumeResult.Message.Value,
                Headers = BuildHeaders(consumeResult, currentRetryCount, error)
            };

            await producer.ProduceAsync(subscription.DeadLetterTopic, deadLetterMessage, cancellationToken);

            _logger.LogError(
                "Routed message with key {Key} from {Topic} to dead-letter topic {DeadLetterTopic} after {RetryCount} retries.",
                consumeResult.Message.Key,
                consumeResult.Topic,
                subscription.DeadLetterTopic,
                currentRetryCount);

            return true;
        }

        _logger.LogError(
            "Message with key {Key} from {Topic} failed and no dead-letter topic is configured. Message will be skipped.",
            consumeResult.Message.Key,
            consumeResult.Topic);

        return true;
    }

    private static Headers BuildHeaders(
        ConsumeResult<string, string> consumeResult,
        int retryCount,
        string? error,
        DateTime? retryAtUtc = null)
    {
        var headers = new Headers();
        var sourceHeaders = consumeResult.Message.Headers;

        if (sourceHeaders is not null)
        {
            foreach (var header in sourceHeaders)
            {
                if (header.Key is RetryCountHeader or OriginalTopicHeader or LastErrorHeader or RetryAtUtcHeader)
                {
                    continue;
                }

                headers.Add(header.Key, header.GetValueBytes());
            }
        }

        var originalTopic = ReadHeader(sourceHeaders, OriginalTopicHeader) ?? consumeResult.Topic;

        headers.Add(RetryCountHeader, Encoding.UTF8.GetBytes(retryCount.ToString(CultureInfo.InvariantCulture)));
        headers.Add(OriginalTopicHeader, Encoding.UTF8.GetBytes(originalTopic));
        if (retryAtUtc.HasValue)
        {
            headers.Add(RetryAtUtcHeader, Encoding.UTF8.GetBytes(retryAtUtc.Value.ToString("O", CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            headers.Add(LastErrorHeader, Encoding.UTF8.GetBytes(Truncate(error, 4000)));
        }

        return headers;
    }

    private static int GetRetryCount(Headers? headers)
    {
        var value = ReadHeader(headers, RetryCountHeader);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static bool TryGetRetryAtUtc(Headers? headers, out DateTime retryAtUtc)
    {
        retryAtUtc = default;

        var value = ReadHeader(headers, RetryAtUtcHeader);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out retryAtUtc);
    }

    private static string? ReadHeader(Headers? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        var header = headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.Ordinal));
        if (header is null)
        {
            return null;
        }

        var valueBytes = header.GetValueBytes();
        return valueBytes is null ? null : Encoding.UTF8.GetString(valueBytes);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private TimeSpan ComputeRetryDelay(int retryCount)
    {
        var baseDelaySeconds = _options.RetryBaseDelaySeconds;
        var maxDelaySeconds = _options.RetryMaxDelaySeconds;

        var exponent = Math.Clamp(retryCount - 1, 0, 30);
        var delaySeconds = baseDelaySeconds * Math.Pow(2, exponent);
        var boundedDelaySeconds = Math.Min(delaySeconds, maxDelaySeconds);

        return TimeSpan.FromSeconds(boundedDelaySeconds);
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
    }

    private static void ValidateOptions(KafkaConsumerRuntimeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.GroupId))
        {
            throw new InvalidOperationException("Kafka group id is not configured.");
        }

        if (options.RetryBaseDelaySeconds <= 0)
        {
            throw new InvalidOperationException("Kafka retry base delay must be greater than zero.");
        }

        if (options.RetryMaxDelaySeconds < options.RetryBaseDelaySeconds)
        {
            throw new InvalidOperationException("Kafka retry max delay must be greater than or equal to base delay.");
        }
    }
}




