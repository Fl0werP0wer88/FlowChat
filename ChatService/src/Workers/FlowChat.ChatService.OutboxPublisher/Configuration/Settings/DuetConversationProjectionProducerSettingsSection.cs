using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class DuetConversationProjectionProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:DuetConversationProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.duet-conversation-projection.v1";
}
