using FlowChat.AuthService.Domain.Common;

namespace FlowChat.AuthService.Domain.Events;

public sealed record AccountConfirmedDomainEvent(
    Guid UserId) : IOutboxDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
