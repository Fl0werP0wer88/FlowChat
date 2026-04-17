using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration;

public sealed class ChatMessageSentProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:ChatMessageSentProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}
