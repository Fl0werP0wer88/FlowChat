using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Entities.Account.Events;

public sealed class AccountRegisteredDomainEvent : BaseAccountDomainEvent
{
    public AccountRegisteredDomainEvent(
        Id<Account> accountId,
        string friendlyUserId,
        EmailAddress email)
        : base(accountId)
    {
        AccountId = accountId;
        FriendlyUserId = friendlyUserId;
        Email = email;
    }

    public Id<Account> AccountId { get; }
    public string FriendlyUserId { get; }
    public EmailAddress Email { get; }
}
