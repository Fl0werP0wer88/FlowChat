using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.HarnessService.Consumers.Configuration.Settings;

public sealed class RetryPipelineConsumerSettingsSection
    : SettingsSectionBase,
        ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:RetryPipelineConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "harness-tiered-retry";
    public string RetryGroupId { get; set; } = "harness-tiered-retry-tiers";
    public string Topic { get; set; } = "test.flowchat.harness.retry.events";
    public string DeadLetterTopic { get; set; } = "test.flowchat.harness.retry.events.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
