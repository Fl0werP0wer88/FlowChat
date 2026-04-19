using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.RealtimeService.Events;

namespace FlowChat.RealtimeService.Infrastructure.Configuration.Settings;

public sealed class RealtimeConnectionUnregisteredProducerSettingsSection : SettingsSectionBase,
    IKafkaProducerSettingsSection<RealtimeConnectionUnregisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:RealtimeConnectionUnregisteredProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.realtime.connection.v1";
}
