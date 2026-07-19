using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;

namespace FlowChat.ChatService.Infrastructure.Configuration.Settings;

public sealed class ConversationV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ProjectionIntegrationEvent<ConversationReadModelV2>>
{
    public override string SectionName => "Kafka:ConversationV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.chat.conversation-projection.v2";
}
