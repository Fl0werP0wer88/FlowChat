using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;

namespace FlowChat.PresenceService.Consumers.Configuration.Settings;

public sealed class ConversationParticipantV2ConsumerSettingsSection
    : SettingsSectionBase,
        ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:ConversationParticipantV2Consumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service";
    public string RetryGroupId { get; set; } = "presence-service-conversation-participant-v2-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.conversation-participant-projection.v2";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
