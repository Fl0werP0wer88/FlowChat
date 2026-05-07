using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration.Settings;

public sealed class UserProfileCreatedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<UserProfileCreatedIntegrationEvent>
{
    public override string SectionName => "Kafka:UserProfileCreatedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
