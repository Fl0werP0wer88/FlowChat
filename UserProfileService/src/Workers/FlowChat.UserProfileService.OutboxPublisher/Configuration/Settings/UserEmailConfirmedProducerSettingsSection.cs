using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;

public sealed class UserEmailConfirmedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:UserEmailConfirmedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
