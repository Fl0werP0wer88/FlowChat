using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class PresenceChangedNotification
{
    public Guid UserId { get; init; }
    public PresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
}
