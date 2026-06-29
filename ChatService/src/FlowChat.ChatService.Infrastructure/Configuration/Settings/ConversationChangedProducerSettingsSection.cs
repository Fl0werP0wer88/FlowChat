using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class ConversationChangedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<ConversationChangedIntegrationEvent>
{
    public override string SectionName => "Kafka:ConversationChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.conversation.v1";
}
