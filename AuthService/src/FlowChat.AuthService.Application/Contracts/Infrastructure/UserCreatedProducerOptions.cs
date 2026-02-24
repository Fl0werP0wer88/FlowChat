using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public sealed class UserCreatedProducerOptions : IKafkaProducerOptions<UserCreatedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserCreatedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
    public Func<UserCreatedIntegrationEvent, string> KeySelector { get; private set; } = (message) => message.UserId.ToString();
}
