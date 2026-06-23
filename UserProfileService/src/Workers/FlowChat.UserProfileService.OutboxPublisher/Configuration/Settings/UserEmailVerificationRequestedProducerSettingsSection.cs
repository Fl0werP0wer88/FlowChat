using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;

public sealed class UserEmailVerificationRequestedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:UserEmailVerificationRequestedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.notification.email.v1";
}
