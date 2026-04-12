namespace FlowChat.PresenceService.OutboxPublisher.Configuration;

public sealed class UserStatusChangedProducerOptions
{
    public const string SectionName = "Kafka:UserStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.user-status-changed.v1";
}
