namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

public sealed record UserProfileDto(
    Guid Id,
    string FriendlyUserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTime? LastSeenAtUtc,
    IReadOnlyList<EmailDto> Emails,
    IReadOnlyList<PhoneDto> Phones);
