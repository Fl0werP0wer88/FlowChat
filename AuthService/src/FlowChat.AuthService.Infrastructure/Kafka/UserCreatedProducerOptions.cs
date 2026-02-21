namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class UserCreatedProducerOptions
{
    public const string SectionName = "Kafka:UserCreatedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "flowchat.identity.user.created.v1";
}
