using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactAddedConsumerOptions : IRetryableKafkaConsumerOptions
{
    public const string SectionName = "Kafka:ContactAddedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service";
    public string RetryGroupId { get; set; } = "presence-service-contact-added-retry";
    public string Topic { get; set; } = "dev.flowchat.social-graph.contact-added.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.social-graph.contact-added.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.social-graph.contact-added.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
