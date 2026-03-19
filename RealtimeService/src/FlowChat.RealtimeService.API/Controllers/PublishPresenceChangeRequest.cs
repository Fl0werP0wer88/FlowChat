namespace FlowChat.RealtimeService.Api.Controllers;

public sealed class PublishPresenceChangeRequest
{
    public Guid UserId { get; init; }
    public string? Status { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
