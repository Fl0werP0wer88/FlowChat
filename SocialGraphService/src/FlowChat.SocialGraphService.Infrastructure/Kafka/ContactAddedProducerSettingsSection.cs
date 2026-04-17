using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public sealed class ContactAddedProducerSettingsSection : IKafkaProducerOptions<ContactAddedIntegrationEvent>
{
    public const string SectionName = "Kafka:ContactAddedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
