using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Domain.Entities.Identity;

[AggregateType(AggregateTypeNames.Identity)]
public abstract class BaseIdentityDomainEvent(Id<Identity> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }

