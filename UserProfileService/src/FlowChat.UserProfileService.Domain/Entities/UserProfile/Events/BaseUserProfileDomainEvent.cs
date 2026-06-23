using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Constants;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

[AggregateType(UserProfileConstants.UserProfileAggregateTypeName)]
public abstract class BaseUserProfileDomainEvent(Id<UserProfile> aggregateId, UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{ }

