using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class AccountRegisteredProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<AccountRegisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:AccountRegisteredProducer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
