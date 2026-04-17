using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration;

public sealed class UserProfileStateChangedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:UserProfileStateChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
