namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed record UserProfileReadModel(
    Guid UserProfileId,
    string UserName,
    string DisplayName,
    string? MainEmail,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    bool IsEmailVisible,
    bool IsPhoneVisible);
