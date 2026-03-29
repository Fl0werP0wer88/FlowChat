namespace FlowChat.AuthService.OutboxPublisher.Configuration;

public sealed class AccountRegisteredProducerOptions
{
    public const string SectionName = "Kafka:AccountRegisteredProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
