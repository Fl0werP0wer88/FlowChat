namespace FlowChat.AuthService.Persistence.Configuration;

public sealed class OutboxPublisherRuntimeOptions
{
    public const string SectionName = "OutboxPublisher";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string[] AllowedTopics { get; set; } = ["dev.flowchat.identity.user.v1"];

    public int BatchSize { get; set; } = 25;

    public int PollIntervalSeconds { get; set; } = 3;

    public int RetryBaseDelaySeconds { get; set; } = 3;

    public int MaxRetryDelaySeconds { get; set; } = 120;
}
