using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.OutboxPublisher.Configuration.Settings;

public sealed class ConversationV2ProducerSettingsSection
    : ProducerSettingsSectionBase,
      IKafkaProducerSettingsSection<ConversationChangedIntegrationEventV2>
{
    public override string SectionName => "Kafka:ConversationV2Producer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    //Review2-11: Publikuj na pierwotny topic. Wydaje mi sie to dobry pomysł jak sądzisz?
    public override string Topic { get; set; } = "dev.flowchat.chat.conversation-v2.v1";
}
