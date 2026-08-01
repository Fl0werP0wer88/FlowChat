using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.AuthService.Consumers.Configuration.Settings;

public sealed class UserEmailConfirmedConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:UserEmailConfirmedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "auth-service";
    public string RetryGroupId { get; set; } = "auth-service-retry";
    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.user-profile.user-profile.v1.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
