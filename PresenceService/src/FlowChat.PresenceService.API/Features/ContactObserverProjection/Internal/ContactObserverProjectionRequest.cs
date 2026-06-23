using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;

public sealed class ContactObserverProjectionRequest : IServiceInput
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
}
