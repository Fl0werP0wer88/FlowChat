using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public sealed class PresenceStatusChangedProducerOptions : IKafkaProducerOptions<PresenceStatusChangedIntegrationEvent>
{
    public const string SectionName = "Kafka:PresenceStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.presence";
}
