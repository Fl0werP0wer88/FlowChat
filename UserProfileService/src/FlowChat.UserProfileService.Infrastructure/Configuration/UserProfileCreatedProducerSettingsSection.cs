using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class UserProfileCreatedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<UserProfileCreatedIntegrationEvent>
{
    public override string SectionName => "Kafka:UserProfileCreatedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
