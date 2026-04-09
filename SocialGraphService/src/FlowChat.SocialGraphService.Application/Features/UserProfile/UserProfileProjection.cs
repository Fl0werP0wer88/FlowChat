using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed record UserProfileProjection(
    Guid UserProfileId,
    string FriendlyUserId,
    string? MainEmail,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    string? FirstName = null,
    string? LastName = null,
    string? Organization = null) : IDbResponse;
