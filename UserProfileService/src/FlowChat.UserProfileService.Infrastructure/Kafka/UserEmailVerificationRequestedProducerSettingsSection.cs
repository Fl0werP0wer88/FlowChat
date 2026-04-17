using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserEmailVerificationRequestedProducerSettingsSection : IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent>
{
    public const string SectionName = "Kafka:UserEmailVerificationRequestedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
