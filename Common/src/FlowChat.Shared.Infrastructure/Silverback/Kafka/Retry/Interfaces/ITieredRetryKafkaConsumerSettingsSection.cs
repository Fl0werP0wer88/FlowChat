namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

public interface ITieredRetryKafkaConsumerSettingsSection
{
    string BootstrapServers { get; }
    string GroupId { get; }
    string RetryGroupId { get; }
    string Topic { get; }
    string DeadLetterTopic { get; }
    IReadOnlyList<RetryTierSettings> RetryTiers { get; }
    string AutoOffsetReset { get; }
}
