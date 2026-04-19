using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration.Settings;

public sealed class AccountRegisteredProducerSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:AccountRegisteredProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
