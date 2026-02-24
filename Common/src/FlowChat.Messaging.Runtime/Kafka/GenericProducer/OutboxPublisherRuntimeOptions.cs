namespace FlowChat.Messaging.Runtime.Kafka.GenericProducer;

public sealed class OutboxPublisherRuntimeOptions
{
    public const string SectionName = "OutboxPublisher";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public List<string> AllowedTopics { get; set; } = [];
    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
}
