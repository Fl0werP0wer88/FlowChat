using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Entities.Account.Events;

[AggregateType(AggregateTypeNames.Account)]
public abstract class BaseAccountDomainEvent(Id<Account> aggregateId, UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}
