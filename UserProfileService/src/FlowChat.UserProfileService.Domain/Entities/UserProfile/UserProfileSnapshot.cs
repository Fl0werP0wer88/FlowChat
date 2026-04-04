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
    DateTime? LastSeenAtUtc,
    bool IsEmailVisible,
    bool IsPhoneVisible);
