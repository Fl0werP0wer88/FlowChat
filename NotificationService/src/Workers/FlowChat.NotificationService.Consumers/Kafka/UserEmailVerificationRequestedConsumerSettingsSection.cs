using FlowChat.Shared.Infrastructure.Silverback.Kafka;

using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.Consumers.Kafka;

public sealed class UserEmailVerificationRequestedConsumerSettingsSection : SettingsSectionBase, IRetryableKafkaConsumerOptions
{
    public override string SectionName => "Kafka:UserEmailVerificationRequestedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "notification-service";
    public string RetryGroupId { get; set; } = "notification-service-retry";
    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.notification.email.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.notification.email.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
