using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class PresenceDto
{
    public Guid UserId { get; init; }
    public PresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
}
