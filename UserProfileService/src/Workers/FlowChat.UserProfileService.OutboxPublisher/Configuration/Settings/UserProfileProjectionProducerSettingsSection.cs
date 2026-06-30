using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;

public sealed class UserProfileProjectionProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:UserProfileProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
