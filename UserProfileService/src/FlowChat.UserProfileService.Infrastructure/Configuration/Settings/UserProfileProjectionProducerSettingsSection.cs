using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;

namespace FlowChat.UserProfileService.Infrastructure.Configuration.Settings;

public sealed class UserProfileProjectionProducerSettingsSection
    : ProducerSettingsSectionBase,
        IKafkaProducerSettingsSection<ProjectionIntegrationEvent<UserProfileReadModel>>
{
    public override string SectionName => "Kafka:UserProfileProjectionProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.user-profile.user-profile-projection.v1";
}
