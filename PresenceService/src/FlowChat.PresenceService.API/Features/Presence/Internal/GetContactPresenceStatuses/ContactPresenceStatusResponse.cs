using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.GetContactPresenceStatuses;

public sealed class ContactPresenceStatusResponse : IServiceOutput
{
    public Guid UserId { get; init; }
    public PresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
}
