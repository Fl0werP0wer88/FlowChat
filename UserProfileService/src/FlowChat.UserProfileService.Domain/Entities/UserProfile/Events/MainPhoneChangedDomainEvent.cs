using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

public sealed class MainPhoneChangedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Phone> phoneId,
    PhoneNumber number,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = aggregateId;
    public Id<Phone> PhoneId { get; } = phoneId;
    public PhoneNumber Number { get; } = number;
}
