using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class DuetConversationProjectionProducerSettingsSection : ProducerSettingsSectionBase,
    IKafkaProducerSettingsSection<ProjectionIntegrationEvent<DuetConversationMembershipReadModel>>,
    IKafkaProducerSettingsSection<ProjectionIntegrationEvent<DuetConversationContactStateReadModel>>
{
    public override string SectionName => "Kafka:DuetConversationProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.duet-conversation-projection.v1";
}
