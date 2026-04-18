using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserEmailVerificationRequestedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<EmailVerificationRequestIntegrationEvent>
{
    public override string SectionName => "Kafka:UserEmailVerificationRequestedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
