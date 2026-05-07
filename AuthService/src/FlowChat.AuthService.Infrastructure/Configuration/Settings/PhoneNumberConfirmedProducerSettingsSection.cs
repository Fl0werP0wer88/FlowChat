using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Infrastructure.Configuration.Settings;

public sealed class PhoneNumberConfirmedProducerSettingsSection : ProducerSettingsSectionBase,
    IKafkaProducerSettingsSection<PhoneNumberConfirmedIntegrationEvent>
{
    public override string SectionName => "Kafka:PhoneNumberConfirmedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
