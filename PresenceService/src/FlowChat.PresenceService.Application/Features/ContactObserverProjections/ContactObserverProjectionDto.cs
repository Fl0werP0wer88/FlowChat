using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections;

public sealed class ContactObserverProjectionDto : IDbReadResponse
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset LastModifiedAtUtc { get; init; }
}
