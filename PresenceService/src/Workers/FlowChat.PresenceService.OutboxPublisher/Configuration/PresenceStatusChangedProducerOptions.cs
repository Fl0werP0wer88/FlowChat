namespace FlowChat.PresenceService.OutboxPublisher.Configuration;

public sealed class PresenceStatusChangedProducerOptions
{
    public const string SectionName = "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence-status-changed.v1";
}
