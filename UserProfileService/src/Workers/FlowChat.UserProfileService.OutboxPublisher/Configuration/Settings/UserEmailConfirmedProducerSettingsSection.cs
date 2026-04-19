using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;

public sealed class UserEmailConfirmedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:UserEmailConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
