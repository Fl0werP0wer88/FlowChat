namespace FlowChat.AuthService.OutboxPublisher.Configuration;

public sealed class UserEmailVerificationRequestedProducerOptions
{
    public const string SectionName = "Kafka:UserEmailVerificationRequestedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
