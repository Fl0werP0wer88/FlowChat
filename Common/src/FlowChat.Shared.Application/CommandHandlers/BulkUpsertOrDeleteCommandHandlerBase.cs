using MediatR;

namespace FlowChat.Shared.Application;

public abstract class BulkUpsertOrDeleteCommandHandlerBase<TCommand, TCommandItem, TValue>
    : TransactionalCommandHandlerBase<TCommand, BulkUpsertOrDeleteCommandResult>
    where TCommand : IBulkUpsertOrDeleteCommand<TCommandItem, TValue>, IRequest<FlowChatResult<BulkUpsertOrDeleteCommandResult>>
    where TCommandItem : IBulkCommandItem<TValue>
    where TValue : class
{
    private readonly IBulkExecutor<TValue> _bulkExecutor;

    protected BulkUpsertOrDeleteCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkExecutor<TValue> bulkExecutor)
        : base(unitOfWork)
    {
        _bulkExecutor = bulkExecutor ?? throw new ArgumentNullException(nameof(bulkExecutor));
    }

    protected override async Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>> ExecuteCommandAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var items = GetItems(request);
        if (items.Count == 0)
            return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Success(BulkUpsertOrDeleteCommandResult.Empty);

        var upsertItems = items.Where(i => i.Value is not null).Select(i => i.Value!).ToArray();
        var deleteIds = items.Where(i => i.Value is null).Select(i => i.EntityId).ToArray();

        int upsertedCount = 0;
        int deletedCount = 0;

        if (upsertItems.Length > 0)
        {
            var upsertResult = await _bulkExecutor.UpsertAsync(upsertItems, cancellationToken);
            if (upsertResult.IsFailure)
                return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Failure(upsertResult.Error);
            upsertedCount = upsertResult.Value;
        }

        if (deleteIds.Length > 0)
        {
            var deleteResult = await _bulkExecutor.DeleteAsync(deleteIds, cancellationToken);
            if (deleteResult.IsFailure)
                return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Failure(deleteResult.Error);
            deletedCount = deleteResult.Value;
        }

        return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Success(
            new BulkUpsertOrDeleteCommandResult(items.Count, upsertedCount, deletedCount));
    }

    protected virtual IReadOnlyCollection<TCommandItem> GetItems(TCommand request) => request.Items;
}
