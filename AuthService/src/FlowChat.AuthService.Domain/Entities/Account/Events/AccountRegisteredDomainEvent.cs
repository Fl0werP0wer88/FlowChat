using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Entities.Account.Events;

public sealed class AccountRegisteredDomainEvent : BaseAccountDomainEvent
{
    public AccountRegisteredDomainEvent(
        Id<Account> accountId,
        string friendlyUserId,
        EmailAddress email,
        string? firstName = null,
        string? lastName = null,
        string? organization = null)
        : base(accountId)
    {
        AccountId = accountId;
        FriendlyUserId = friendlyUserId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Organization = organization;
    }

    public Id<Account> AccountId { get; }
    public string FriendlyUserId { get; }
    public EmailAddress Email { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? Organization { get; }
}
