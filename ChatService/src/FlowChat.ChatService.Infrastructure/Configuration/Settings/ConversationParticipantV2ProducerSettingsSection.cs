using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class ConversationParticipantV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>>
{
    public override string SectionName => "Kafka:ConversationParticipantV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } =
        "dev.flowchat.chat.conversation-participant-projection.v2";
}
