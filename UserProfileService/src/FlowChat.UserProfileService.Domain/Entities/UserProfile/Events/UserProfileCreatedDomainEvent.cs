using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

public sealed class UserProfileCreatedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> mainEmailId,
    string friendlyUserId,
    string displayName,
    EmailAddress mainEmail,
    PhoneNumber? mainPhone,
    string? avatarUrl,
    string? bio,
    bool isActive,
    UtcDateTimeOffset? lastSeenAtUtc,
    string? firstName = null,
    string? lastName = null,
    string? organization = null,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = aggregateId;
    public Id<Email> MainEmailId { get; } = mainEmailId;
    public string FriendlyUserId { get; } = friendlyUserId;
    public string DisplayName { get; } = displayName;
    public EmailAddress MainEmail { get; } = mainEmail;
    public PhoneNumber? MainPhone { get; } = mainPhone;
    public string? AvatarUrl { get; } = avatarUrl;
    public string? Bio { get; } = bio;
    public bool IsActive { get; } = isActive;
    public UtcDateTimeOffset? LastSeenAtUtc { get; } = lastSeenAtUtc;
    public string? FirstName { get; } = firstName;
    public string? LastName { get; } = lastName;
    public string? Organization { get; } = organization;
}
