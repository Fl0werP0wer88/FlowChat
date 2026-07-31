namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

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

public sealed class RetryTierSettings
{
    public string Topic { get; set; } = string.Empty;
    public TimeSpan Delay { get; set; }
}
