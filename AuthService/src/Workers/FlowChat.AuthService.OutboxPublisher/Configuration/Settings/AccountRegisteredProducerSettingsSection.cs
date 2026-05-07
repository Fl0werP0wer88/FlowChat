using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration.Settings;

public sealed class AccountRegisteredProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:AccountRegisteredProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";
    public override string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
