using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionBulkRepository
{
    Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken);
}
