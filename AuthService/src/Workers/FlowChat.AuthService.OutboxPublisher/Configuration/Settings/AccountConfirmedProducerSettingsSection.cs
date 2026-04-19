using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration.Settings;

public sealed class AccountConfirmedProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:AccountConfirmedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
