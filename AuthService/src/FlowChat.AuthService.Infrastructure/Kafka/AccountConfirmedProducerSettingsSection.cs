using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class AccountConfirmedProducerSettingsSection : SettingsSectionBase, IKafkaProducerSettingsSection<AccountConfirmedIntegrationEvent>
{
    public override string SectionName => "Kafka:AccountConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
