using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandler
    : ProjectionBulkCommandHandlerBase<
        BulkUpsertOrDeleteUserProfileProjectionCommand,
        UserProfileProjectionCommandItem,
        IUserProfileProjectionBulkRepository>
{
    public BulkUpsertOrDeleteUserProfileProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IUserProfileProjectionBulkRepository bulkRepository)
        : base(unitOfWork, bulkRepository)
    {
    }
}
