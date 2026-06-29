using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.RealtimeService.Consumers.Configuration.Settings;

public sealed class ConversationChangedConsumerSettingsSection : SettingsSectionBase, IRetryableKafkaConsumerSettingsSection
{
    public override string SectionName => "Kafka:ConversationChangedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "realtime-service";
    public string RetryGroupId { get; set; } = "realtime-service-retry";
    public string Topic { get; set; } = "dev.flowchat.chat.conversation.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.chat.conversation.v1.realtime-service.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.chat.conversation.v1.realtime-service.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
