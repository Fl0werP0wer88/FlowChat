using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class GroupConversationProjectionProducerSettingsSection : ProducerSettingsSectionBase,
    IKafkaProducerSettingsSection<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>
{
    public override string SectionName => "Kafka:GroupConversationProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.chat.group-conversation-projection.v1";
}
