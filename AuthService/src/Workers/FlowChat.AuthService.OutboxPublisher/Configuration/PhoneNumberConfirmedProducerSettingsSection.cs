using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration;

public sealed class PhoneNumberConfirmedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:PhoneNumberConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
