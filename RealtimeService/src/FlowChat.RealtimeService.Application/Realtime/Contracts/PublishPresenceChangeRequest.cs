namespace FlowChat.RealtimeService.Application.Realtime.Contracts;

public sealed class PublishPresenceChangeRequest
{
    public Guid UserId { get; init; }
    public string? Status { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
