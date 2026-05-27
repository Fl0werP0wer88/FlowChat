using FlowChat.Shared.Application;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;

public sealed record BulkUpsertUserProfileProjectionCommand(
    IReadOnlyCollection<BulkUpsertUserProfileProjectionCommandItem> Items)
    : IBulkUpsertCommand<BulkUpsertUserProfileProjectionCommandItem>;

public sealed record BulkUpsertUserProfileProjectionCommandItem(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? MainEmailAddress,
    bool? MainEmailIsConfirmed,
    bool? MainEmailIsVisible,
    string? MainPhoneNumber,
    bool? MainPhoneIsConfirmed,
    bool? MainPhoneIsVisible,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc);
