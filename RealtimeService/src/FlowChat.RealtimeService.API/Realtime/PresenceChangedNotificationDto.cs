using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class PresenceChangedNotificationDto
{
    public Guid UserId { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
}
