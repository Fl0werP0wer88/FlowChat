using FlowChat.AuthService.Domain.Common;

namespace FlowChat.AuthService.Domain.Events;

public sealed record EmailConfirmedDomainEvent(
    Guid UserId,
    string Email) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
