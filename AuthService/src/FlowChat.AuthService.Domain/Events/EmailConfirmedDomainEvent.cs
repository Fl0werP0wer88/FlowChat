using FlowChat.AuthService.Domain.Entities;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Events;

public sealed class EmailConfirmedDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }
    public string Email { get; }

    public EmailConfirmedDomainEvent(
        Id<Identity> userId,
        string email)
        : base(userId)
    {
        UserId = userId;
        Email = email;
    }
}

