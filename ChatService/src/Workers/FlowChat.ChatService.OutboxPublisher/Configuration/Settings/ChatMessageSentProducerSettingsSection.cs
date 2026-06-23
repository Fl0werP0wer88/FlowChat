using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class ChatMessageSentProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:ChatMessageSentProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.message.v1";
}
