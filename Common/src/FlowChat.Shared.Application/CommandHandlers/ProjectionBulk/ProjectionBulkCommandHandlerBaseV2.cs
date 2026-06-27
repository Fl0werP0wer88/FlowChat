using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public class ProjectionBulkCommandHandlerBaseV2<TCommand, TItem, TRepository>
    : TransactionalCommandHandlerBase<TCommand, Unit>
    where TCommand : IProjectionBulkCommand<TItem>
    where TItem : notnull
    where TRepository : IProjectionBulkRepository<TItem>
{
    private readonly TRepository _bulkRepository;
    private readonly IProjectionOffsetStore _offsetStore;

    public ProjectionBulkCommandHandlerBaseV2(
        IUnitOfWork unitOfWork,
        TRepository bulkRepository,
        IProjectionOffsetStore offsetStore)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
        _offsetStore = offsetStore ?? throw new ArgumentNullException(nameof(offsetStore));
    }

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count > 0)
            await _bulkRepository.BulkUpsertOrSoftDeleteAsync(request.Items, cancellationToken);

        // Commit the broker offset inside the UoW transaction so projection writes and offset advancement complete together
        await _offsetStore.CommitConsumedOffsetsAsync(cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
