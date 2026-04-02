using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Events;

public sealed class AccountRegisteredDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }
    public string UserName { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }
    public string? FirstName { get; }
    public string? LastName { get; }

    public AccountRegisteredDomainEvent
    (
        Id<Identity> userId,
        string userName,
        string? email,
        string? phoneNumber,
        string? firstName,
        string? lastName) : base(userId)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
        PhoneNumber = phoneNumber;
        FirstName = firstName;
        LastName = lastName;
    }
}

