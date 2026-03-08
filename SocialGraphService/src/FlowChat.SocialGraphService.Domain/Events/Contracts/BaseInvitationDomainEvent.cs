using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Common.Constants;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Domain.Events.Contracts;

[AggregateType(InvitationConstants.InvitationAggregateTypeName)]
public abstract class BaseInvitationDomainEvent(Id<Invitation> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }
