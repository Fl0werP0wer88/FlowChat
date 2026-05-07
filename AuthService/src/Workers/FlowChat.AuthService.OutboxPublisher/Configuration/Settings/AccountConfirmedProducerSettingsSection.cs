using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.OutboxPublisher.Configuration.Settings;

public sealed class AccountConfirmedProducerSettingsSection : ProducerSettingsSectionBase
{
    public override string SectionName => "Kafka:AccountConfirmedProducer";

    public override string BootstrapServers { get; set; } = "localhost:9092";

    public override string Topic { get; set; } = "dev.flowchat.identity.user.v1";
}
