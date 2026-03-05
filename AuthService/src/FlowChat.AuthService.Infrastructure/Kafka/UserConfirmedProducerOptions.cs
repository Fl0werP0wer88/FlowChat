using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class UserConfirmedProducerOptions : IKafkaProducerOptions<UserConfirmedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserConfirmedProducer";
    public const string FallbackSectionName = UserCreatedProducerOptions.SectionName;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
