using System;
using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Common.Constants;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Events.Decorators;

namespace FlowChat.SocialGraphService.Domain.Events.Contracts;

[AggregateType(UserSocialGraphConstants.SocialGraphAggregateTypeName)]
public abstract class BaseCouponDomainEvent(Id<UserSocialGraph> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEvent(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }