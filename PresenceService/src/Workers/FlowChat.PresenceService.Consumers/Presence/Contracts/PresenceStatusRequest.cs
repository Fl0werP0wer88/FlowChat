using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Presence.Contracts;

public sealed class PresenceStatusRequest : IConsumerOutput
{
    public Guid UserId { get; init; }
}
