namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class UserPresenceChangedConsumerOptions
{
    public const string SectionName = "Kafka:UserPresenceChangedConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "realtime-service";
    public string Topic { get; set; } = "dev.flowchat.user-profile.presence-changed.v1";
    public string RetryTopic { get; set; } = "dev.flowchat.user-profile.presence-changed.v1.retry";
    public string DeadLetterTopic { get; set; } = "dev.flowchat.user-profile.presence-changed.v1.dlq";
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int RetryMaxDelaySeconds { get; set; } = 300;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
