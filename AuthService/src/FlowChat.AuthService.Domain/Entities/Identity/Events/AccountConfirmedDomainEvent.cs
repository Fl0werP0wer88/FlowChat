using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Entities.Identity.Events;

public sealed class AccountConfirmedDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }

    public AccountConfirmedDomainEvent(Id<Identity> userId)
        : base(userId)
    {
        UserId = userId;
    }
}

