using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.RoutePresenceChange;

public sealed class RoutePresenceChangeRequest : IServiceInput
{
    public Guid UserId { get; init; }
    public PresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
