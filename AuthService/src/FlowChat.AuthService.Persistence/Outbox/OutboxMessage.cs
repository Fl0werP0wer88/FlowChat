namespace FlowChat.AuthService.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Key { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Headers { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryOnUtc { get; set; }
    public string? Error { get; set; }
}
