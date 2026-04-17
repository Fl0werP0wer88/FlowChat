using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserProfileCreatedProducerSettingsSection : SettingsSectionBase, IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>
{
    public override string SectionName => "Kafka:UserProfileCreatedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
