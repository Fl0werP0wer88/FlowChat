using FlowChat.Core.Results;
using FlowChat.HarnessService.Application.Contracts.Infrastructure;
using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;

public sealed class BulkUpsertProjectionCommandHandler
    : ProjectionBulkCommandHandlerBase<
        BulkUpsertProjectionCommand,
        ProjectionCommandItem,
        IProjectionTestBulkRepository>
{
    private readonly IProjectionTestBulkRepository _bulkRepository;
    private readonly IConsumerOffsetStore _offsetStore;

    public BulkUpsertProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IProjectionTestBulkRepository bulkRepository,
        IConsumerOffsetStore offsetStore)
        : base(unitOfWork, bulkRepository)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
        _offsetStore = offsetStore ?? throw new ArgumentNullException(nameof(offsetStore));
    }

    // Commits the Kafka offset via Silverback's own KafkaOffsetStoreScope before this method's
    // transaction commits, so the offset commit and the projection write succeed or roll back
    // together (see ConsumerOffsetStore).
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        BulkUpsertProjectionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count > 0)
            await _bulkRepository.BulkUpsertOrSoftDeleteAsync(request.Items, cancellationToken);

        await _offsetStore.CommitConsumedOffsetsAsync(cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
