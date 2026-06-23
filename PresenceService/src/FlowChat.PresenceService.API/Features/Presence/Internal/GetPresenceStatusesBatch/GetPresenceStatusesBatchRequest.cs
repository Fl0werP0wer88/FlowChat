using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.GetPresenceStatusesBatch;

public sealed class GetPresenceStatusesBatchRequest : IServiceInput
{
    public IReadOnlyCollection<Guid> UserIds { get; init; } = [];
}
