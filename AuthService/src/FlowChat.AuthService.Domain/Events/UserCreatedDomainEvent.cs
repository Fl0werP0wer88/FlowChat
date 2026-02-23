using FlowChat.AuthService.Domain.Common;

namespace FlowChat.AuthService.Domain.Events;

public sealed record UserCreatedDomainEvent(
    Guid UserId,
    string UserName,
    string DisplayName,
    string Email) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
