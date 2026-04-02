using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.AuthService.Domain.Entities.Identity.Events;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Entities.Identity;

public sealed class Identity : AggregateRootBase<Identity>
{
    public string UserName { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }
    public bool EmailConfirmed { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool AccountConfirmed { get; private set; }
    public string? FirstName { get; }
    public string? LastName { get; }


    private Identity(
        Guid id,
        string userName,
        string? email,
        string? phoneNumber,
        bool emailConfirmed,
        bool phoneNumberConfirmed,
        string? firstName,
        string? lastName) : base(id)
    {
        UserName = userName;
        Email = email;
        PhoneNumber = phoneNumber;
        EmailConfirmed = emailConfirmed;
        PhoneNumberConfirmed = phoneNumberConfirmed;
        AccountConfirmed = emailConfirmed || phoneNumberConfirmed;
        FirstName = firstName;
        LastName = lastName;
    }

    public static Identity Create(Guid id, string userName, string email)
        => Create(id, userName, email, null);

    public static Identity Create(Guid id, string userName, string? email, string? phoneNumber)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName is required.", nameof(userName));
        }

        var normalizedUserName = userName.Trim();
        var normalizedEmail = NormalizeOptional(email);
        var normalizedPhoneNumber = NormalizeOptional(phoneNumber);

        if (normalizedEmail is null && normalizedPhoneNumber is null)
        {
            throw new ArgumentException("Either email or phone number is required.");
        }

        var user = new Identity(
            id,
            normalizedUserName,
            normalizedEmail,
            normalizedPhoneNumber,
            emailConfirmed: false,
            phoneNumberConfirmed: false,
            firstName: null,
            lastName: null);

        user.AddDomainEvent(new AccountRegisteredDomainEvent(
            user.Id,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.FirstName,
            user.LastName));

        return user;
    }

    public static Identity Restore(
        Guid id,
        string userName,
        string? email,
        string? phoneNumber,
        bool emailConfirmed,
        bool phoneNumberConfirmed,
        string? firstName = null,
        string? lastName = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName is required.", nameof(userName));
        }

        var normalizedUserName = userName.Trim();
        var normalizedEmail = NormalizeOptional(email);
        var normalizedPhoneNumber = NormalizeOptional(phoneNumber);

        if (normalizedEmail is null && normalizedPhoneNumber is null)
        {
            throw new ArgumentException("Either email or phone number is required.");
        }

        return new Identity(
            id,
            normalizedUserName,
            normalizedEmail,
            normalizedPhoneNumber,
            emailConfirmed,
            phoneNumberConfirmed,
            NormalizeOptional(firstName),
            NormalizeOptional(lastName));
    }

    public void ConfirmEmail()
    {
        var email = Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email confirmation requires an email address.");
        }

        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;

        if (!AccountConfirmed)
        {
            AccountConfirmed = true;
            AddDomainEvent(new AccountConfirmedDomainEvent(Id));
        }
    }

    public void ConfirmPhone()
    {
        var phoneNumber = PhoneNumber;
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new InvalidOperationException("Phone confirmation requires a phone number.");
        }

        if (!PhoneNumberConfirmed)
        {
            PhoneNumberConfirmed = true;
            AddDomainEvent(new PhoneNumberConfirmedDomainEvent(Id, phoneNumber));
        }

        if (!AccountConfirmed)
        {
            AccountConfirmed = true;
            AddDomainEvent(new AccountConfirmedDomainEvent(Id));
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

