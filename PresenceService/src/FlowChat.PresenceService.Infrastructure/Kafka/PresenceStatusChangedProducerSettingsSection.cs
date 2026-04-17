using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public sealed class PresenceStatusChangedProducerSettingsSection : SettingsSectionBase, IKafkaProducerOptions<PresenceStatusChangedIntegrationEvent>
{
    public override string SectionName => "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence";
}
