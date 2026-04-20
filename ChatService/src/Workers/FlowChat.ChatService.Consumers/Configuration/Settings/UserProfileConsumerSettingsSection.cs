using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.ChatService.Consumers.Configuration.Settings;

public sealed class UserProfileConsumerSettingsSection : SettingsSectionBase, IRetryableKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:UserProfileConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "chat-service";
    public string RetryGroupId { get; set; } = "chat-service-retry";
    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.user-profile.user-profile.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.user-profile.user-profile.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
