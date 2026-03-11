namespace FlowChat.SocialGraphService.Application.UserProfiles;

public sealed record UserProfileReadModel(
    Guid UserProfileId,
    string UserName,
    string DisplayName,
    string? MainEmail,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTime? LastSeenAtUtc,
    bool IsEmailVisible,
    bool IsPhoneVisible);
