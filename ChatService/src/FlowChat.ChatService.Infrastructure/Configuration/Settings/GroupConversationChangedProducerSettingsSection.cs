using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class GroupConversationChangedProducerSettingsSection : ProducerSettingsSectionBase,
    IKafkaProducerSettingsSection<GroupConversationChangedIntegrationEvent>,
    IKafkaProducerSettingsSection<GroupConversationParticipantsAddedIntegrationEvent>,
    IKafkaProducerSettingsSection<GroupConversationParticipantsRemovedIntegrationEvent>
{
    public override string SectionName => "Kafka:GroupConversationChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.group-conversation.v1";
}
