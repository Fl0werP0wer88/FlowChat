namespace FlowChat.SocialGraphService.OutboxPublisher.Configuration;

public sealed class ContactDeletedProducerOptions
{
    public const string SectionName = "Kafka:ContactDeletedProducer";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "dev.flowchat.social-graph.contact";
}
