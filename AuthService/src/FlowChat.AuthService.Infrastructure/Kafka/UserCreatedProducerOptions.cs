using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class UserCreatedProducerOptions : IKafkaProducerOptions<UserCreatedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserCreatedProducer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
