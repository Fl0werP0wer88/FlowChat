using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Configuration.Settings;

public sealed class UserEmailConfirmedProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<UserEmailConfirmedIntegrationEvent>
{
    public override string SectionName => "Kafka:UserEmailConfirmedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
