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
    //Review2-10: Publikuj na pierwotny topic. Wydaje mi sie to dobry pomysł jak sądzisz?
    public override string Topic { get; set; } =
        "dev.flowchat.chat.conversation-membership-v2-projection.v1";
}
