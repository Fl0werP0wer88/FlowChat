using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionBulkRepository
{
    Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>> BulkUpsertOrDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken);
}
