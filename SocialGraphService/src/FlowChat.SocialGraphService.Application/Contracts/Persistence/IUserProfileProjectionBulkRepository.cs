using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IUserProfileProjectionBulkRepository
{
    Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken);
}
