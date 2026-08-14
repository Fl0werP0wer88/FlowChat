namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public static class RetryMessageHeaders
{
    public const string RetryAttempt = "retry-attempt";
    public const string RetryAtUtc = "retry-at-utc";
    public const string FirstFailedAtUtc = "first-failed-at-utc";
    public const string OriginalTopic = "original-topic";
    public const string OriginalPartition = "original-partition";
    public const string OriginalOffset = "original-offset";
    public const string LastErrorType = "last-error-type";

    internal const string InvalidRetryMetadata = "flowchat-invalid-retry-metadata";
}
