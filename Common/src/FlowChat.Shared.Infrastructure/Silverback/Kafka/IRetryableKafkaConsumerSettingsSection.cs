namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public interface IRetryableKafkaConsumerSettingsSection
{
    string Topic { get; }
    string RetryTopic { get; }
    string DeadLetterTopic { get; }
    int MaxRetryCount { get; }
    int RetryBaseDelaySeconds { get; }
    int RetryMaxDelaySeconds { get; }
}
