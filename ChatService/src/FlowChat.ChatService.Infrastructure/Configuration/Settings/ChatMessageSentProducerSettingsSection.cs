using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class ChatMessageSentProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<ChatMessageSentIntegrationEvent>
{
    public override string SectionName => "Kafka:ChatMessageSentProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}
