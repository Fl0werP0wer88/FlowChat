using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.Messaging.Runtime.Kafka.GenericProducer;

public sealed class GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage> : BackgroundService
    where TDbContext : DbContext
    where TOutboxMessage : class, IOutboxMessage
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly OutboxPublisherRuntimeOptions _options;
    private readonly HashSet<string> _allowedTopics;
    private readonly ILogger<GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage>> _logger;

    public GenericKafkaOutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<OutboxPublisherRuntimeOptions> options,
        ILogger<GenericKafkaOutboxPublisherBackgroundService<TDbContext, TOutboxMessage>> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _allowedTopics = _options.AllowedTopics
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (_allowedTopics.Count == 0)
        {
            throw new InvalidOperationException("No outbox topics configured in OutboxPublisher:AllowedTopics.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Generic outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing outbox messages.");
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        return base.StopAsync(cancellationToken);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, _options.BatchSize);

        var messages = await dbContext.Set<TOutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .Where(message => message.NextRetryOnUtc == null || message.NextRetryOnUtc <= now)
            .Where(message => _allowedTopics.Contains(message.Topic))
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        foreach (var message in messages)
        {
            await PublishMessageAsync(message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }

    private async Task PublishMessageAsync(TOutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = message.Key ?? string.Empty,
                Value = message.Content,
                Headers = DeserializeHeaders(message.Headers)
            };

            var result = await _producer.ProduceAsync(message.Topic, kafkaMessage, cancellationToken);

            message.ProcessedOnUtc = DateTime.UtcNow;
            message.NextRetryOnUtc = null;
            message.Error = null;

            _logger.LogInformation(
                "Published outbox message {OutboxMessageId} of type {Type} to {TopicPartitionOffset}",
                message.Id,
                message.Type,
                result.TopicPartitionOffset);
        }
        catch (Exception ex)
        {
            message.RetryCount += 1;
            message.Error = ex.ToString();
            message.NextRetryOnUtc = DateTime.UtcNow.AddSeconds(CalculateRetryDelaySeconds(message.RetryCount));

            _logger.LogWarning(
                ex,
                "Failed to publish outbox message {OutboxMessageId}. Retry {RetryCount} scheduled for {NextRetryOnUtc}",
                message.Id,
                message.RetryCount,
                message.NextRetryOnUtc);
        }
    }

    private Headers? DeserializeHeaders(string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return null;
        }

        try
        {
            var headersDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
            if (headersDictionary is null || headersDictionary.Count == 0)
            {
                return null;
            }

            var headers = new Headers();
            foreach (var header in headersDictionary)
            {
                headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
            }

            return headers;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Headers JSON for outbox message is invalid. Message will be published without headers.");
            return null;
        }
    }

    private int CalculateRetryDelaySeconds(int retryCount)
    {
        var baseDelay = Math.Max(1, _options.RetryBaseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, _options.MaxRetryDelaySeconds);

        var exponentialDelay = baseDelay * Math.Pow(2, Math.Min(retryCount, 8));
        return (int)Math.Min(maxDelay, exponentialDelay);
    }
}
