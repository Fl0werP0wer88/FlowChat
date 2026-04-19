using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;

public sealed class UserProfileCreatedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:UserProfileCreatedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
