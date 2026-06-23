using FlowChat.Core.Messaging.PresenceService.Events;

using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Infrastructure.Configuration.Settings;

public sealed class PresenceStatusChangedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<PresenceStatusChangedIntegrationEvent>
{
    public override string SectionName => "Kafka:PresenceStatusChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.presence.presence";
}
