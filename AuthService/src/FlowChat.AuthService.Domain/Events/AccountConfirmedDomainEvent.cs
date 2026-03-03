using FlowChat.AuthService.Domain.Entities;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Events;

public sealed class AccountConfirmedDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }

    public AccountConfirmedDomainEvent(Id<Identity> userId)
        : base(userId)
    {
        UserId = userId;
    }
}
