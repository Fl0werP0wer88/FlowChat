using MediatR;

namespace FlowChat.Shared.Application;

public abstract class BulkUpsertOrDeleteCommandHandlerBase<TCommand, TValue>
    : TransactionalCommandHandlerBase<TCommand, Unit>
    where TCommand : IBulkUpsertOrDeleteCommand<TValue>
    where TValue : class
{
    private readonly IBulkRepository<TValue> _bulkRepository;

    protected BulkUpsertOrDeleteCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkRepository<TValue> bulkRepository)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
    }

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var items = GetItems(request);
        if (items.Count == 0)
            return FlowChatResult<Unit>.Success(Unit.Value);

        var upsertItems = items.Where(i => i.Value is not null).Select(i => i.Value!).ToArray();
        var deleteIds = items.Where(i => i.Value is null).Select(i => i.EntityId).ToArray();

        if (upsertItems.Length > 0)
        {
            var upsertResult = await _bulkRepository.BulkUpsertAsync(upsertItems, cancellationToken);
            if (upsertResult.IsFailure)
                return FlowChatResult<Unit>.Failure(upsertResult.Error);
        }

        if (deleteIds.Length > 0)
        {
            var deleteResult = await _bulkRepository.BulkDeleteAsync(deleteIds, cancellationToken);
            if (deleteResult.IsFailure)
                return FlowChatResult<Unit>.Failure(deleteResult.Error);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected virtual IReadOnlyCollection<BulkCommandItem<TValue>> GetItems(TCommand request) => request.Items;
}
