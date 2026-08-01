using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.OutboxPublisher.Configuration.Settings;

public sealed class RetryOutboxKafkaSettingsSection : SettingsSectionBase
{
    public override string SectionName => "Kafka:RetryOutbox";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public IReadOnlyList<string> Topics { get; set; } = [];
}
