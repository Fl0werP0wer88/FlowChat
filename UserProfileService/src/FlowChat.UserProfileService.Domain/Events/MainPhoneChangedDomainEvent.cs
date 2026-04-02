using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

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
