using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Events;

public sealed class UserCreatedDomainEvent: BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; } 
    string UserName { get; } 
    string DisplayName { get; } 
    string Email { get; } 

    public UserCreatedDomainEvent
    (
        Id<Identity> userId, 
        string userName,
        string displayName,
        string email): base (userId)
    {
        UserId = userId;
        UserName = userName;
        DisplayName = displayName;
        Email = email;
    }
}