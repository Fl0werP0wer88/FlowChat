namespace FlowChat.Messaging.Runtime.Kafka.GenericProducer;

public interface IOutboxMessage
{
    Guid Id { get; }
    string Type { get; }
    string Topic { get; }
    string? Key { get; }
    string Content { get; }
    string? Headers { get; }
    DateTime OccurredOnUtc { get; }
    DateTime? ProcessedOnUtc { get; set; }
    int RetryCount { get; set; }
    DateTime? NextRetryOnUtc { get; set; }
    string? Error { get; set; }
}
