using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class ConversationChangedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:ConversationChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.conversation.v1";
}
