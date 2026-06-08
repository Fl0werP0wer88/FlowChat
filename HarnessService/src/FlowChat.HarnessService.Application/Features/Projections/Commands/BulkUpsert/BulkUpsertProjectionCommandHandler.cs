using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;

namespace FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;

public sealed class BulkUpsertProjectionCommandHandler
    : ProjectionBulkCommandHandlerBase<
        BulkUpsertProjectionCommand,
        ProjectionCommandItem,
        IProjectionTestBulkRepository>
{
    public BulkUpsertProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IProjectionTestBulkRepository bulkRepository)
        : base(unitOfWork, bulkRepository)
    {
    }
}
