using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;

namespace FlowChat.AuthService.Domain.Entities;

public sealed class IdentityEntity : AggregateRoot
{
    public Guid Id { get; }
    public string UserName { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public bool EmailConfirmed { get; private set; }
    public bool AccountConfirmed { get; private set; }

    private IdentityEntity(Guid id, string userName, string email, string displayName)
    {
        Id = id;
        UserName = userName;
        Email = email;
        DisplayName = displayName;
        EmailConfirmed = false;
        AccountConfirmed = false;
    }

    public static IdentityEntity Create(Guid id, string userName, string email)
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

        var user = new IdentityEntity(
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

    public void ConfirmEmail()
    {
        if (!EmailConfirmed)
        {
            EmailConfirmed = true;
            AddDomainEvent(new EmailConfirmedDomainEvent(Id, Email));
        }

        if (!AccountConfirmed)
        {
            AccountConfirmed = true;
            AddDomainEvent(new AccountConfirmedDomainEvent(Id));
        }
    }
}
