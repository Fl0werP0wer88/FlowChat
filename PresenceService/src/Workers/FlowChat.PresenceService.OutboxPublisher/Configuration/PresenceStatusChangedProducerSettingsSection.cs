using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.OutboxPublisher.Configuration;

public sealed class PresenceStatusChangedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence";
}
