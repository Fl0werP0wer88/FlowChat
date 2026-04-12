using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Presence.Contracts;

public sealed class ContactObserverProjectionRequest : IConsumerOutput
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
}
