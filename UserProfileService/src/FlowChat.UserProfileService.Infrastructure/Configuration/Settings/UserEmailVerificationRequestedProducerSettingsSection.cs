using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration.Settings;

public sealed class UserEmailVerificationRequestedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<EmailVerificationRequestIntegrationEvent>
{
    public override string SectionName => "Kafka:UserEmailVerificationRequestedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
