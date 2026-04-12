namespace FlowChat.SocialGraphService.OutboxPublisher.Configuration;

public sealed class OutboxPublisherRuntimeOptions
{
    public const string SectionName = "OutboxPublisher";

    public int BatchSize { get; set; } = 25;
    public int PollIntervalSeconds { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 120;
}
