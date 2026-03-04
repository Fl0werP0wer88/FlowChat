using FlowChat.AuthService.Domain.Common.Constants;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Entities;

[AggregateType(AggregateTypeNames.Identity)]
public abstract class BaseIdentityDomainEvent(Id<Identity> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }
