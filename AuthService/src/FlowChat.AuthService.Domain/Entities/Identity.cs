using FlowChat.AuthService.Domain.Common;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Entities;

public sealed class Identity : AggregateRootBase<Identity>
{
    public string UserName { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public bool EmailConfirmed { get; private set; }
    public bool AccountConfirmed { get; private set; }

    private Identity(Guid id, string userName, string email, string displayName) : base (id)
    {
        UserName = userName;
        Email = email;
        DisplayName = displayName;
        EmailConfirmed = false;
        AccountConfirmed = false;
    }

    public static Identity Create(Guid id, string userName, string email)
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

        var user = new Identity(
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
