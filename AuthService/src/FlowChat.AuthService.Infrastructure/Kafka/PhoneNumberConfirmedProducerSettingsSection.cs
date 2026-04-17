using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public sealed class PhoneNumberConfirmedProducerSettingsSection : SettingsSectionBase,
    IKafkaProducerSettingsSection<PhoneNumberConfirmedIntegrationEvent>
{
    public override string SectionName => "Kafka:PhoneNumberConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
