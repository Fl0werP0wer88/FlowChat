using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

public sealed class UserProfileCreatedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> mainEmailId,
    string userName,
    string displayName,
    EmailAddress mainEmail,
    PhoneNumber? mainPhone,
    string? avatarUrl,
    string? bio,
    bool isActive,
    DateTime? lastSeenAtUtc,
    bool isEmailVisible,
    bool isPhoneVisible,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = aggregateId;
    public Id<Email> MainEmailId { get; } = mainEmailId;
    public string UserName { get; } = userName;
    public string DisplayName { get; } = displayName;
    public EmailAddress MainEmail { get; } = mainEmail;
    public PhoneNumber? MainPhone { get; } = mainPhone;
    public string? AvatarUrl { get; } = avatarUrl;
    public string? Bio { get; } = bio;
    public bool IsActive { get; } = isActive;
    public DateTime? LastSeenAtUtc { get; } = lastSeenAtUtc;
    public bool IsEmailVisible { get; } = isEmailVisible;
    public bool IsPhoneVisible { get; } = isPhoneVisible;
}
