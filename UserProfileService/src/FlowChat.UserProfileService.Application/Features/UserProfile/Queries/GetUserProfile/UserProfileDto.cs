using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

public sealed record UserProfileDto(
    Guid Id,
    string FriendlyUserId,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    IReadOnlyList<EmailDto> Emails,
    IReadOnlyList<PhoneDto> Phones) : IDbResponse;
