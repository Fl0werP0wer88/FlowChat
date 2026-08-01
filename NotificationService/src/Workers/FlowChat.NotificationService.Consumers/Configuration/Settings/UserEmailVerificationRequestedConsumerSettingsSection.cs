using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.NotificationService.Consumers.Configuration.Settings;

public sealed class UserEmailVerificationRequestedConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:UserEmailVerificationRequestedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "notification-service";
    public string RetryGroupId { get; set; } = "notification-service-retry";
    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.notification.email.v1.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
