using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.OutboxPublisher.Configuration.Settings;

public sealed class PresenceStatusChangedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:PresenceStatusChangedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.presence.presence";
}
