using System.Globalization;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace FlowChat.Messaging.Runtime.Kafka;

public sealed class TopicSubscription<TEvent> : ITopicSubscription
{
    private readonly Func<TEvent, MessageContext, IServiceProvider, CancellationToken, Task<MessageHandlingResult>> _handler;
    private readonly JsonSerializerOptions _serializerOptions;

    public TopicSubscription(
        string topic,
        Func<TEvent, MessageContext, IServiceProvider, CancellationToken, Task<MessageHandlingResult>> handler,
        string? retryTopic = null,
        string? deadLetterTopic = null,
        int maxRetryCount = 5,
        JsonSerializerOptions? serializerOptions = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        Topic = topic;
        RetryTopic = retryTopic;
        DeadLetterTopic = deadLetterTopic;
        MaxRetryCount = Math.Max(0, maxRetryCount);
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public string Topic { get; }
    public string? RetryTopic { get; }
    public string? DeadLetterTopic { get; }
    public int MaxRetryCount { get; }

    public IEnumerable<string> TopicsToSubscribe
    {
        get
        {
            yield return Topic;

            if (!string.IsNullOrWhiteSpace(RetryTopic))
            {
                yield return RetryTopic;
            }
        }
    }

    public async Task<MessageHandlingResult> HandleAsync(
        ConsumeResult<string, string> consumeResult,
        IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken)
    {
        if (consumeResult.Message?.Value is null)
        {
            return MessageHandlingResult.Skip("Message payload is empty.");
        }

        TEvent? message;

        try
        {
            message = JsonSerializer.Deserialize<TEvent>(consumeResult.Message.Value, _serializerOptions);
        }
        catch (JsonException ex)
        {
            return MessageHandlingResult.Skip($"Invalid JSON payload: {ex.Message}");
        }

        if (message is null)
        {
            return MessageHandlingResult.Skip("Deserialized event is null.");
        }

        var headers = ExtractHeaders(consumeResult.Message.Headers);
        var context = new MessageContext(
            consumeResult.Topic,
            consumeResult.Message.Key,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            GetRetryCount(headers),
            headers.TryGetValue(GenericKafkaConsumerBackgroundService.OriginalTopicHeader, out var originalTopic)
                ? originalTopic
                : null,
            headers);

        return await _handler(message, context, scopedServiceProvider, cancellationToken);
    }

    private static Dictionary<string, string> ExtractHeaders(Headers? headers)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (headers is null)
        {
            return result;
        }

        foreach (var header in headers)
        {
            var bytes = header.GetValueBytes();
            if (bytes is null)
            {
                continue;
            }

            result[header.Key] = Encoding.UTF8.GetString(bytes);
        }

        return result;
    }

    private static int GetRetryCount(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue(GenericKafkaConsumerBackgroundService.RetryCountHeader, out var value))
        {
            return 0;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }
}
