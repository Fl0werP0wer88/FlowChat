using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public sealed class RealtimeConnectionProducerSettingsSection : SettingsSectionBase,
    IKafkaProducerOptions<RealtimeConnectionRegisteredIntegrationEvent>,
    IKafkaProducerOptions<RealtimeConnectionUnregisteredIntegrationEvent>
{
    public override string SectionName => "Kafka:RealtimeConnectionProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.realtime.connection.v1";
}
