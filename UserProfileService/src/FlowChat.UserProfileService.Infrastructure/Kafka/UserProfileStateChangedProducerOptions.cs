using FlowChat.Messaging.Contracts.UserProfileService.Events;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserProfileStateChangedProducerOptions : IKafkaProducerOptions<UserProfileStateChangedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserProfileStateChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
