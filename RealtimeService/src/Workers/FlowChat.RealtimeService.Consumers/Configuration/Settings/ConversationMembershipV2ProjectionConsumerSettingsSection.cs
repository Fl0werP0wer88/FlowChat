using FlowChat.Core.Contracts;
namespace FlowChat.RealtimeService.Consumers.Configuration.Settings;

public sealed class ConversationMembershipV2ProjectionConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:ConversationMembershipV2ProjectionConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "realtime-service";
    public string RetryGroupId { get; set; } = "realtime-service-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.conversation-membership-projection.v2";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
