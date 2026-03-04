using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Events;

public sealed class UserCreatedDomainEvent: BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }
    public string UserName { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }

    public UserCreatedDomainEvent
    (
        Id<Identity> userId,
        string userName,
        string? email,
        string? phoneNumber) : base(userId)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
        PhoneNumber = phoneNumber;
    }
}
