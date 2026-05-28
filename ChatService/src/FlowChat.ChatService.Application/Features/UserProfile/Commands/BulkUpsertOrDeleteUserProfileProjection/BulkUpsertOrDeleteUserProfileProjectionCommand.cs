using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed record BulkUpsertOrDeleteUserProfileProjectionCommand(
    IReadOnlyCollection<BulkCommandItem<UserProfileProjectionDto>> Items)
    : IBulkUpsertOrDeleteCommand<UserProfileProjectionDto>;
