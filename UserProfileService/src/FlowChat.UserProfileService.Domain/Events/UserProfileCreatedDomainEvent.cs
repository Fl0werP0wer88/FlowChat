using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class UserProfileCreatedDomainEvent(
    Id<UserProfile> aggregateId,
    string userName,
    string displayName,
    string? mainEmail,
    string? mainPhone,
    string? avatarUrl,
    string? bio,
    bool isActive,
    DateTime? lastSeenAtUtc,
    bool isEmailVisible,
    bool isPhoneVisible,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid UserProfileId { get; } = aggregateId.Value;
    public string UserName { get; } = userName;
    public string DisplayName { get; } = displayName;
    public string? MainEmail { get; } = mainEmail;
    public string? MainPhone { get; } = mainPhone;
    public string? AvatarUrl { get; } = avatarUrl;
    public string? Bio { get; } = bio;
    public bool IsActive { get; } = isActive;
    public DateTime? LastSeenAtUtc { get; } = lastSeenAtUtc;
    public bool IsEmailVisible { get; } = isEmailVisible;
    public bool IsPhoneVisible { get; } = isPhoneVisible;
}
