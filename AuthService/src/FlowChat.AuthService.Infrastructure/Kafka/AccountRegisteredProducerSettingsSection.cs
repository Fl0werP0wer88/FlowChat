using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class AccountRegisteredProducerSettingsSection : SettingsSectionBase, IKafkaProducerOptions<AccountRegisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:AccountRegisteredProducer";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
