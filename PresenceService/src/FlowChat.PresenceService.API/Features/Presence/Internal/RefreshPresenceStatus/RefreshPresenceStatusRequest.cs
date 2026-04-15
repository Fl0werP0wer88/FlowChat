using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.RefreshPresenceStatus;

public sealed class RefreshPresenceStatusRequest : IServiceInput
{
    public IReadOnlyCollection<Guid> UserIds { get; init; } = [];
}
