using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.UserProfileService.Consumers.Configuration.Settings;

public sealed class AccountRegisteredConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:AccountRegisteredConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "userprofile-service";
    public string RetryGroupId { get; set; } = "userprofile-service-retry";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.identity.user.v1.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
