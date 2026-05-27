using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;

public sealed record BulkUpsertUserProfileProjectionCommand(
    IReadOnlyCollection<BulkUpsertUserProfileProjectionCommandItem> Items)
    : IBulkUpsertCommand<BulkUpsertUserProfileProjectionCommandItem>;

public sealed record BulkUpsertUserProfileProjectionCommandItem(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? DisplayName,
    string? AvatarUrl);
