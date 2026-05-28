using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandler(
    IUnitOfWork unitOfWork,
    IBulkRepository<UserProfileProjectionDto> bulkRepository)
    : BulkUpsertOrDeleteCommandHandlerBase<
        BulkUpsertOrDeleteUserProfileProjectionCommand,
        UserProfileProjectionDto>(unitOfWork, bulkRepository);

