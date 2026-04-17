using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public sealed class RealtimeConnectionRegisteredProducerSettingsSection : SettingsSectionBase,
    IKafkaProducerSettingsSection<RealtimeConnectionRegisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:RealtimeConnectionRegisteredProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.realtime.connection.v1";
}
