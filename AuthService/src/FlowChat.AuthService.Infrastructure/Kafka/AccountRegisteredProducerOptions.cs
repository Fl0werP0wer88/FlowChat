using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class AccountRegisteredProducerOptions : IKafkaProducerOptions<AccountRegisteredIntegrationEvent>
{
    public const string SectionName = "Kafka:AccountRegisteredProducer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
