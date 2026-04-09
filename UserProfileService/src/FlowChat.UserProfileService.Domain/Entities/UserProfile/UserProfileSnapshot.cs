using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public sealed record UserProfileSnapshot(
    Guid UserProfileId,
    string FriendlyUserId,
    string? MainEmail,
    bool? IsMainEmailConfirmed,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    UtcDateTimeOffset? LastSeenAtUtc,
    string? FirstName = null,
    string? LastName = null,
    string? Organization = null);
