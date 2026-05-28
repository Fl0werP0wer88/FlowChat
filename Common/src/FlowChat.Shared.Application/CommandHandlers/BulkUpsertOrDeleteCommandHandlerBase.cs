using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class BulkUpsertOrDeleteCommandHandlerBase<TCommand, TCommandItem, TExecutorItem>
    : TransactionalCommandHandlerBase<TCommand, BulkUpsertOrDeleteCommandResult>
    where TCommand : IBulkUpsertOrDeleteCommand<TCommandItem>, IRequest<FlowChatResult<BulkUpsertOrDeleteCommandResult>>
    where TCommandItem : IBulkCommandItem
    where TExecutorItem : notnull
{
    private readonly IBulkExecutor<TExecutorItem> _bulkExecutor;

    protected BulkUpsertOrDeleteCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkExecutor<TExecutorItem> bulkExecutor)
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

        var upsertItems = items.Where(i => !i.MarkedForDeletion).Select(MapItem).ToArray();
        var deleteItems = items.Where(i => i.MarkedForDeletion).Select(MapItem).ToArray();

        int upsertedCount = 0;
        int deletedCount = 0;

        if (upsertItems.Length > 0)
        {
            var upsertResult = await _bulkExecutor.UpsertAsync(upsertItems, cancellationToken);
            if (upsertResult.IsFailure)
                return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Failure(upsertResult.Error);
            upsertedCount = upsertResult.Value;
        }

        if (deleteItems.Length > 0)
        {
            var deleteResult = await _bulkExecutor.DeleteAsync(deleteItems, cancellationToken);
            if (deleteResult.IsFailure)
                return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Failure(deleteResult.Error);
            deletedCount = deleteResult.Value;
        }

        return FlowChatResult<BulkUpsertOrDeleteCommandResult>.Success(
            new BulkUpsertOrDeleteCommandResult(items.Count, upsertedCount, deletedCount));
    }

    protected virtual IReadOnlyCollection<TCommandItem> GetItems(TCommand request) => request.Items;

    protected abstract TExecutorItem MapItem(TCommandItem item);
}

public abstract class BulkUpsertOrDeleteCommandHandlerBase<TCommand, TItem>
    : BulkUpsertOrDeleteCommandHandlerBase<TCommand, TItem, TItem>
    where TCommand : IBulkUpsertOrDeleteCommand<TItem>, IRequest<FlowChatResult<BulkUpsertOrDeleteCommandResult>>
    where TItem : IBulkCommandItem
{
    protected BulkUpsertOrDeleteCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkExecutor<TItem> bulkExecutor)
        : base(unitOfWork, bulkExecutor)
    {
    }

    protected override TItem MapItem(TItem item) => item;
}
