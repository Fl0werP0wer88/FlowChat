using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Entities.Account;

public sealed class Account : AggregateRootBase<Account>
{
    private Account(
        Id<Account>? id,
        string friendlyUserId,
        EmailAddress email,
        string passwordHash,
        string securityStamp,
        int accessFailedCount,
        bool isEmailConfirmed)
        : base(id)
    {
        FriendlyUserId = friendlyUserId;
        Email = email;
        PasswordHash = passwordHash;
        SecurityStamp = securityStamp;
        AccessFailedCount = accessFailedCount;
        IsEmailConfirmed = isEmailConfirmed;
    }

    public string FriendlyUserId { get; private set; }
    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; }
    public int AccessFailedCount { get; private set; }
    public bool IsEmailConfirmed { get; private set; }

    public static Account Create(
        string friendlyUserId,
        EmailAddress email,
        string passwordHash,
        string securityStamp,
        string? firstName = null,
        string? lastName = null,
        string? organization = null,
        Id<Account>? id = null)
    {
        ArgumentNullException.ThrowIfNull(email);

        var account = new Account(
            id ?? Id<Account>.New(),
            NormalizeRequired(friendlyUserId, nameof(friendlyUserId)),
            email,
            NormalizeRequired(passwordHash, nameof(passwordHash)),
            NormalizeRequired(securityStamp, nameof(securityStamp)),
            accessFailedCount: 0,
            isEmailConfirmed: false);

        account.AddDomainEvent(new AccountRegisteredDomainEvent(
            account.Id,
            account.FriendlyUserId,
            account.Email,
            NormalizeOptional(firstName),
            NormalizeOptional(lastName),
            NormalizeOptional(organization)));
        account.MarkAggregateStateChanged(AggregateTypeNames.Account, account.CreateSnapshot);

        return account;
    }

    public static Account Restore(
        Guid id,
        string friendlyUserId,
        EmailAddress email,
        string passwordHash,
        string securityStamp,
        int accessFailedCount,
        bool isEmailConfirmed)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Account id is required.", nameof(id));
        }

        if (accessFailedCount < 0)
        {
            throw new ArgumentException("AccessFailedCount cannot be negative.", nameof(accessFailedCount));
        }

        return new Account(
            new Id<Account>(id),
            NormalizeRequired(friendlyUserId, nameof(friendlyUserId)),
            email,
            NormalizeRequired(passwordHash, nameof(passwordHash)),
            NormalizeRequired(securityStamp, nameof(securityStamp)),
            accessFailedCount,
            isEmailConfirmed);
    }

    public void ConfirmEmail()
    {
        // Idempotent — email links may be clicked more than once or the event replayed.
        if (IsEmailConfirmed)
        {
            return;
        }

        IsEmailConfirmed = true;
        AddDomainEvent(new AccountConfirmedDomainEvent(Id));
        MarkAggregateStateChanged(AggregateTypeNames.Account, CreateSnapshot);
    }

    public void RecordFailedLogin()
    {
        AccessFailedCount++;
        MarkAggregateStateChanged(AggregateTypeNames.Account, CreateSnapshot);
    }

    public void ResetFailedLogins()
    {
        // Skip the state change event when already at zero — avoids a no-op snapshot on every successful login.
        if (AccessFailedCount == 0)
        {
            return;
        }

        AccessFailedCount = 0;
        MarkAggregateStateChanged(AggregateTypeNames.Account, CreateSnapshot);
    }

    public void RotateSecurityStamp(string securityStamp)
    {
        // A new security stamp invalidates all previously issued tokens that embed the old stamp.
        // Must be called on password change, email change, or explicit sign-out-everywhere.
        SecurityStamp = NormalizeRequired(securityStamp, nameof(securityStamp));
        MarkAggregateStateChanged(AggregateTypeNames.Account, CreateSnapshot);
    }

    private AccountSnapshot CreateSnapshot()
    {
        return new AccountSnapshot(
            Id.Value,
            FriendlyUserId,
            Email.Value,
            SecurityStamp,
            AccessFailedCount,
            IsEmailConfirmed);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
