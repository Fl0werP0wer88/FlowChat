namespace FlowChat.Workers.Abstractions.Kafka;

public interface IRetryableKafkaConsumerOptions
{
    string Topic { get; }
    string RetryTopic { get; }
    string DeadLetterTopic { get; }
    int MaxRetryCount { get; }
    int RetryBaseDelaySeconds { get; }
    int RetryMaxDelaySeconds { get; }
}
