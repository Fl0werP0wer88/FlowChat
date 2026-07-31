namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public sealed class RetryTierSettings
{
    public string Topic { get; set; } = string.Empty;
    public TimeSpan Delay { get; set; }
}
