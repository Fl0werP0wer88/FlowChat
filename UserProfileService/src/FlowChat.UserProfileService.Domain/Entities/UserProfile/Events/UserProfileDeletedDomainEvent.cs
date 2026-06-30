using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

public sealed class UserProfileDeletedDomainEvent(
    Id<UserProfile> userProfileId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseUserProfileDomainEvent(userProfileId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = userProfileId;
}
