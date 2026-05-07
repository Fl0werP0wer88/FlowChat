using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Infrastructure.Configuration.Settings;

public sealed class AccountRegisteredProducerSettingsSection : ProducerSettingsSectionBase, IKafkaProducerSettingsSection<AccountRegisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:AccountRegisteredProducer";
    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
