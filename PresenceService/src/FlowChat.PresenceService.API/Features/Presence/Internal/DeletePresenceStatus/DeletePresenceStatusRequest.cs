using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.DeletePresenceStatus;

public sealed class DeletePresenceStatusRequest : IServiceInput
{
    public Guid UserId { get; init; }
}
