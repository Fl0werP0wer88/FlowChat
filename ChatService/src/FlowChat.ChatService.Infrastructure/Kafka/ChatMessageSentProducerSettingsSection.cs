using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public sealed class ChatMessageSentProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<ChatMessageSentIntegrationEvent>
{
    public override string SectionName => "Kafka:ChatMessageSentProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}
