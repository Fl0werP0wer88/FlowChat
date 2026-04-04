using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Entities.Account.Events;

[AggregateType(AggregateTypeNames.Account)]
public abstract class BaseAccountDomainEvent(Id<Account> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{
}
