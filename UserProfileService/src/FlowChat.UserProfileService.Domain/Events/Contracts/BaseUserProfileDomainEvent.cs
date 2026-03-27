using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Common.Constants;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Domain.Events.Contracts;

[AggregateType(UserProfileConstants.UserProfileAggregateTypeName)]
public abstract class BaseUserProfileDomainEvent(Id<UserProfile> aggregateId, DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{ }

