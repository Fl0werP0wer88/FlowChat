using FlowChat.Core.Messaging.PresenceService.Events;

using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Infrastructure.Configuration.Settings;

public sealed class PresenceStatusChangedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<PresenceStatusChangedIntegrationEvent>
{
    public override string SectionName => "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence";
}
