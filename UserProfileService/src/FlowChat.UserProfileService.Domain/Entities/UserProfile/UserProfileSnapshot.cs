using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public sealed record UserProfileSnapshot(
    Guid UserProfileId,
    string FriendlyUserId,
    string DisplayName,
    string? MainEmail,
    bool? IsMainEmailConfirmed,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    UtcDateTimeOffset? LastSeenAtUtc,
    bool IsEmailVisible,
    bool IsPhoneVisible);
