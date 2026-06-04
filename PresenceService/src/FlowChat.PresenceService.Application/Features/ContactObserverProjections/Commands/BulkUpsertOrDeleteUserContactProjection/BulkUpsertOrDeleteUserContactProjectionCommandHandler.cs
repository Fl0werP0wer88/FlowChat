using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionCommandHandler
    : ProjectionBulkCommandHandlerBase<
        BulkUpsertOrDeleteUserContactProjectionCommand,
        UserContactProjectionCommandItem,
        IContactObserverProjectionBulkRepository>
{
    public BulkUpsertOrDeleteUserContactProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IContactObserverProjectionBulkRepository bulkRepository)
        : base(unitOfWork, bulkRepository)
    {
    }
}
