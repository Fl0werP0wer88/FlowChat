using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Infrastructure.Kafka;

public sealed class UserEmailConfirmedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<UserEmailConfirmedIntegrationEvent>
{
    public override string SectionName => "Kafka:UserEmailConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.user-profile.user-profile.v1";
}
