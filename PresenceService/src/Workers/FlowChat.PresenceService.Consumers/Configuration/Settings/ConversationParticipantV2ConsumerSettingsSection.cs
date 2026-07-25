using FlowChat.Core.Contracts;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.PresenceService.Consumers.Configuration.Settings;

public sealed class ConversationParticipantV2ConsumerSettingsSection : SettingsSectionBase, IProjectionBulkConsumerSettingsSection
{
    public override string SectionName => "Kafka:ConversationParticipantV2Consumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service";
    public string RetryGroupId { get; set; } = "presence-service-conversation-participant-v2-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.conversation-participant-projection.v2";
    public string RetryTopic { get; set; } = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
    public int BatchSize { get; set; } = 100;
    public int BatchMaxWaitTimeMilliseconds { get; set; } = 1000;
}
