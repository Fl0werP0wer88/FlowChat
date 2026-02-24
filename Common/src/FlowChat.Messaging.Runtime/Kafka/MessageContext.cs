namespace FlowChat.Messaging.Runtime.Kafka;

public sealed record MessageContext(
    string Topic,
    string? Key,
    int Partition,
    long Offset,
    int RetryCount,
    string? OriginalTopic,
    IReadOnlyDictionary<string, string> Headers);
