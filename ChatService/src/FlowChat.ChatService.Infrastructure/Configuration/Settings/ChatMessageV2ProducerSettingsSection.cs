using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class ChatMessageV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ChatMessageSentIntegrationEventV2>
{
    public override string SectionName => "Kafka:ChatMessageV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.chat.message.v2";
}
