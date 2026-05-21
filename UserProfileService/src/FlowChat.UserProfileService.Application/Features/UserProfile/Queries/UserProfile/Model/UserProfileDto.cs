using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record UserProfileDto(
    Guid Id,
    string FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    IReadOnlyList<EmailDto> Emails,
    IReadOnlyList<PhoneDto> Phones) : IDbReadResponse;
