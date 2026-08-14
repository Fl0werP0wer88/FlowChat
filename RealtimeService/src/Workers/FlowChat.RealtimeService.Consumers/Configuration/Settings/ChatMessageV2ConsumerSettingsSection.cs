using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
namespace FlowChat.RealtimeService.Consumers.Configuration.Settings;

public sealed class ChatMessageV2ConsumerSettingsSection : SettingsSectionBase, ITieredRetryKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:ChatMessageV2Consumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "realtime-service";
    public string RetryGroupId { get; set; } = "realtime-service-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.message.v2";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.message.v2.realtime-service.dlq";
    public IReadOnlyList<RetryTierSettings> RetryTiers { get; set; } = [];
    public string AutoOffsetReset { get; set; } = "Earliest";
}
