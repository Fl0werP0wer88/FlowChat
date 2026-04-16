using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Constants;

namespace FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

[AggregateType(ContactConstants.ContactAggregateTypeName)]
public abstract class BaseContactDomainEvent(Id<Contact> aggregateId, UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow);
