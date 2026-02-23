using FlowChat.AuthService.Application.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public sealed class UserCreatedProducerOptions : IKafkaProducerOptions<UserCreatedEvent>
{
    public const string SectionName = "Kafka:UserCreatedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "flowchat.identity.user.created.v1";
    public Func<UserCreatedEvent, string> KeySelector { get; private set; } = (message) => message.UserId.ToString();
}
