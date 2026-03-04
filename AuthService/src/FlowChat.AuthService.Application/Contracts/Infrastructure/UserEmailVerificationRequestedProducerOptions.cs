using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public sealed class UserEmailVerificationRequestedProducerOptions : IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent>
{
    public const string SectionName = "Kafka:UserEmailVerificationRequestedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedProducer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
