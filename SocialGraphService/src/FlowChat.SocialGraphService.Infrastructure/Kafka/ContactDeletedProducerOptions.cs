using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public sealed class ContactDeletedProducerOptions : IKafkaProducerOptions<ContactDeletedIntegrationEvent>
{
    public const string SectionName = "Kafka:ContactDeletedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
