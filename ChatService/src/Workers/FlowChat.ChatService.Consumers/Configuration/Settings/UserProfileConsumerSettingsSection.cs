using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.ChatService.Consumers.Configuration.Settings;

public sealed class UserProfileConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:UserProfileConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "chat-service";
    public string RetryGroupId { get; set; } = "chat-service-retry";
    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile-projection.v1";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
