namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

public sealed record UserProfileDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTime? LastSeenAtUtc,
    IReadOnlyList<EmailDto> Emails,
    IReadOnlyList<PhoneDto> Phones);
