using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.API.Features.Presence.Public.ChangePresenceStatus;

public sealed class ChangePresenceStatusRequest : IServiceInput
{
    public PresenceStatus Status { get; init; }
}
