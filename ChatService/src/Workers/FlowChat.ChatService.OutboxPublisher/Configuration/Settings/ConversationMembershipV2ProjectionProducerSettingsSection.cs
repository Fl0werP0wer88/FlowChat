using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class ConversationMembershipV2ProjectionProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>
{
    public override string SectionName => "Kafka:ConversationMembershipV2ProjectionProducer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } =
        "dev.flowchat.chat.conversation-membership-projection.v2";
}
