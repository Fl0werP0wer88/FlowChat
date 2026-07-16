using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class GroupConversationProjectionProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:GroupConversationProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.group-conversation-projection.v1";
}
