using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class UserCreatedProducerOptions : IKafkaProducerOptions<UserCreatedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserCreatedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedConsumer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
