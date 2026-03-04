using FlowChat.AuthService.Domain.Entities;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Events;

public sealed class PhoneNumberConfirmedDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }
    public string PhoneNumber { get; }

    public PhoneNumberConfirmedDomainEvent(
        Id<Identity> userId,
        string phoneNumber)
        : base(userId)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
    }
}
