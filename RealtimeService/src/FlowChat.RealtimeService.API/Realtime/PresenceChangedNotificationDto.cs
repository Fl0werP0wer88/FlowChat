namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class PresenceChangedNotificationDto
{
    public Guid UserId { get; init; }
    public required string Status { get; init; }
    public DateTime ChangedAtUtc { get; init; }
}
