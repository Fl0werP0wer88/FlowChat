namespace FlowChat.PresenceService.OutboxPublisher.Configuration;

public sealed class PresenceStatusChangedProducerSettingsSection
{
    public const string SectionName = "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence";
}
