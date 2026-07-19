using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;


public sealed class ChatMessageV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ChatMessageSentIntegrationEventV2>
{
    public override string SectionName => "Kafka:ChatMessageV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.chat.message.v2";
}
