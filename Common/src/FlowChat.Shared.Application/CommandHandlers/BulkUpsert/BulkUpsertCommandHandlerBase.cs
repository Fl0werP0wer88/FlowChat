using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class BulkUpsertCommandHandlerBase<TCommand, TCommandItem, TUpsertItem>
    : TransactionalCommandHandlerBase<TCommand, BulkUpsertCommandResult>
    where TCommand : IBulkUpsertCommand<TCommandItem>, IRequest<FlowChatResult<BulkUpsertCommandResult>>
    where TCommandItem : notnull
    where TUpsertItem : notnull
{
    private readonly IBulkUpsertExecutor<TUpsertItem> _bulkUpsertExecutor;

    protected BulkUpsertCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkUpsertExecutor<TUpsertItem> bulkUpsertExecutor)
        : base(unitOfWork)
    {
        _bulkUpsertExecutor = bulkUpsertExecutor
            ?? throw new ArgumentNullException(nameof(bulkUpsertExecutor));
    }

    protected override Task<FlowChatResult<BulkUpsertCommandResult>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var items = GetItems(request);
        if (items.Count == 0)
        {
            return Task.FromResult(FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.Empty));
        }

        return _bulkUpsertExecutor.UpsertAsync(
            items.Select(MapItem).ToArray(),
            cancellationToken);
    }

    protected virtual IReadOnlyCollection<TCommandItem> GetItems(TCommand request)
    {
        return request.Items;
    }

    protected abstract TUpsertItem MapItem(TCommandItem item);
}

public abstract class BulkUpsertCommandHandlerBase<TCommand, TItem>
    : BulkUpsertCommandHandlerBase<TCommand, TItem, TItem>
    where TCommand : IBulkUpsertCommand<TItem>, IRequest<FlowChatResult<BulkUpsertCommandResult>>
    where TItem : notnull
{
    protected BulkUpsertCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkUpsertExecutor<TItem> bulkUpsertExecutor)
        : base(unitOfWork, bulkUpsertExecutor)
    {
    }

    protected override TItem MapItem(TItem item) => item;
}
