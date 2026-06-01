using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed record BulkUpsertOrDeleteUserProfileProjectionCommand(
    IReadOnlyCollection<UserProfileProjectionCommandItem> Items)
    : ICommand<BulkUpsertOrDeleteCommandResult>;

public sealed record UserProfileProjectionCommandItem(
    Id<UserProfileProjectionDto> EntityId,
    UserProfileProjectionDto? Value,
    int SourceVersion);
