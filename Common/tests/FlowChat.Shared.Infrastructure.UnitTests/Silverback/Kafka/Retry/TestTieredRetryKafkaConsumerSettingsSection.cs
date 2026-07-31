using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

internal sealed class TestTieredRetryKafkaConsumerSettingsSection : ITieredRetryKafkaConsumerSettingsSection
{
    public string BootstrapServers { get; init; } = "localhost:9092";
    public string GroupId { get; init; } = "test-main";
    public string RetryGroupId { get; init; } = "test-retry";
    public string Topic { get; init; } = "main-topic";
    public string DeadLetterTopic { get; init; } = "dlq-topic";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; init; } = [];
    public string AutoOffsetReset { get; init; } = "Earliest";

    public static TestTieredRetryKafkaConsumerSettingsSection Create() => new()
    {
        RetryTiers =
        [
            new RetryTierSettings { Topic = "retry-5s", Delay = TimeSpan.FromSeconds(5) },
            new RetryTierSettings { Topic = "retry-20s", Delay = TimeSpan.FromSeconds(20) },
            new RetryTierSettings { Topic = "retry-60s", Delay = TimeSpan.FromSeconds(60) },
            new RetryTierSettings { Topic = "retry-300s", Delay = TimeSpan.FromSeconds(300) }
        ]
    };
}
