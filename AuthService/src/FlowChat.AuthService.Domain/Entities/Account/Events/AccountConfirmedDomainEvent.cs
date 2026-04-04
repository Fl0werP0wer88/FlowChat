using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Entities.Account.Events;

public sealed class AccountConfirmedDomainEvent : BaseAccountDomainEvent
{
    public AccountConfirmedDomainEvent(Id<Account> accountId)
        : base(accountId)
    {
        AccountId = accountId;
    }

    public Id<Account> AccountId { get; }
}
