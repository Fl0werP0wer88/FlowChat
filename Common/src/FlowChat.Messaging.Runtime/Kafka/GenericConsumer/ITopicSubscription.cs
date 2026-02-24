using Confluent.Kafka;

namespace FlowChat.Messaging.Runtime.Kafka.GenericConsumer;

public interface ITopicSubscription
{
    string Topic { get; }
    string? RetryTopic { get; }
    string? DeadLetterTopic { get; }
    int MaxRetryCount { get; }
    IEnumerable<string> TopicsToSubscribe { get; }

    Task<MessageHandlingResult> HandleAsync(
        ConsumeResult<string, string> consumeResult,
        IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken);
}

