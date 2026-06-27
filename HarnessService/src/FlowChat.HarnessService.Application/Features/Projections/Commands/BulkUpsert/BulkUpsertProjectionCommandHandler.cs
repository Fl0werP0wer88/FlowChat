using FlowChat.HarnessService.Application.Contracts.Infrastructure;
using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;

namespace FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;

public sealed class BulkUpsertProjectionCommandHandler
    : ProjectionBulkCommandHandlerBaseV2<
        BulkUpsertProjectionCommand,
        ProjectionCommandItem,
        IProjectionTestBulkRepository,
        IConsumerOffsetStore>
{
    public BulkUpsertProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IProjectionTestBulkRepository bulkRepository,
        IConsumerOffsetStore offsetStore)
        : base(unitOfWork, bulkRepository, offsetStore)
    {
    }
}
