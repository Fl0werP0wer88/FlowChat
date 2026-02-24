using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;

namespace FlowChat.AuthService.Domain.Entities;

public sealed class UserEntity : DomainEntity
{
    public Guid Id { get; }
    public string UserName { get; }
    public string Email { get; }
    public string DisplayName { get; }

    private UserEntity(Guid id, string userName, string email, string displayName)
    {
        Id = id;
        UserName = userName;
        Email = email;
        DisplayName = displayName;
    }

    public static UserEntity Create(Guid id, string userName, string email)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName is required.", nameof(userName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedUserName = userName.Trim();
        var normalizedEmail = email.Trim();

        var user = new UserEntity(
            id,
            normalizedUserName,
            normalizedEmail,
            normalizedUserName);

        user.AddDomainEvent(new UserCreatedDomainEvent(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email));

        return user;
    }
}
