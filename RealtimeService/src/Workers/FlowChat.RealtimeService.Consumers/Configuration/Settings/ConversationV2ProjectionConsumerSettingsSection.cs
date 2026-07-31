using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
namespace FlowChat.RealtimeService.Consumers.Configuration.Settings;

public sealed class ConversationV2ProjectionConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:ConversationV2ProjectionConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "realtime-service";
    public string RetryGroupId { get; set; } = "realtime-service-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.conversation-projection.v2";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.conversation-projection.v2.realtime-service.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
