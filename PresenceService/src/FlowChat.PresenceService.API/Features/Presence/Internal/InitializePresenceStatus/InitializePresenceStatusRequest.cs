using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.InitializePresenceStatus;

public sealed class InitializePresenceStatusRequest : IServiceInput
{
    public Guid UserId { get; init; }
}
