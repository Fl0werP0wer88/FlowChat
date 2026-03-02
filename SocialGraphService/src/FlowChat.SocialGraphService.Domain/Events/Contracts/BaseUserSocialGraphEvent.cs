using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Common.Constants;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Domain.Events.Contracts;

[AggregateType(UserSocialGraphConstants.SocialGraphAggregateTypeName)]
public abstract class BaseUserSocialGraphDomainEvent(Id<UserSocialGraph> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEvent(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }
