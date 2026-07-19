using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class ConversationParticipantV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>>
{
    public override string SectionName => "Kafka:ConversationParticipantV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    //Review2-11: Publikuj na pierwotny topic. Wydaje mi sie to dobry pomysł jak sądzisz?
    public override string Topic { get; set; } =
        "dev.flowchat.chat.conversation-participant-v2-projection.v1";
}
