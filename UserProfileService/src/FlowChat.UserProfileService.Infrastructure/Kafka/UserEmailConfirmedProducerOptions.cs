using FlowChat.Core.Messaging.UserProfileService.Events;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserEmailConfirmedProducerOptions : IKafkaProducerOptions<UserEmailConfirmedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserEmailConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
