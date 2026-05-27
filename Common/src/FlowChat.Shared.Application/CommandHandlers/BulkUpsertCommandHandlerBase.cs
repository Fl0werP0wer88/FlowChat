using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class BulkUpsertCommandHandlerBase<TCommand, TItem>
    : TransactionalCommandHandlerBase<TCommand, BulkUpsertCommandResult>
    where TCommand : IBulkUpsertCommand<TItem>, IRequest<FlowChatResult<BulkUpsertCommandResult>>
    where TItem : notnull
{
    private readonly IBulkUpsertExecutor<TItem> _bulkUpsertExecutor;

    protected BulkUpsertCommandHandlerBase(
        IUnitOfWork unitOfWork,
        IBulkUpsertExecutor<TItem> bulkUpsertExecutor)
        : base(unitOfWork)
    {
        _bulkUpsertExecutor = bulkUpsertExecutor
            ?? throw new ArgumentNullException(nameof(bulkUpsertExecutor));
    }

    protected override Task<FlowChatResult<BulkUpsertCommandResult>> ExecuteCommandAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var items = GetItems(request);
        if (items.Count == 0)
        {
            return Task.FromResult(FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.Empty));
        }

        return _bulkUpsertExecutor.UpsertAsync(items, cancellationToken);
    }

    protected virtual IReadOnlyCollection<TItem> GetItems(TCommand request)
    {
        return request.Items;
    }
}
