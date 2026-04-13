using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class RealtimeConnectionConsumerOptions : IRetryableKafkaConsumerOptions
{
    public const string SectionName = "Kafka:RealtimeConnectionConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "presence-service-realtime-connection";
    public string RetryGroupId { get; set; } = "presence-service-realtime-connection-retry";
    public string Topic { get; set; } = "dev.flowchat.realtime.connection.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.realtime.connection.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.realtime.connection.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
