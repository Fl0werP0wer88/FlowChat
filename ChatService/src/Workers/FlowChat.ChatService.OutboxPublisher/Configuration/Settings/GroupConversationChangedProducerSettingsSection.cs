using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class GroupConversationChangedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:GroupConversationChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.group-conversation.v1";
}
