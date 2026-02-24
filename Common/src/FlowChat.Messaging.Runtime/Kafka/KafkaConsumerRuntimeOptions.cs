namespace FlowChat.Messaging.Runtime.Kafka;

public sealed class KafkaConsumerRuntimeOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = string.Empty;
    public string AutoOffsetReset { get; set; } = "Earliest";
}
