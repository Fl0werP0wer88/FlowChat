using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public sealed class UserStatusChangedProducerOptions : IKafkaProducerOptions<UserStatusChangedIntegrationEvent>
{
    public const string SectionName = "Kafka:UserStatusChangedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.presence.user-status-changed.v1";
}
