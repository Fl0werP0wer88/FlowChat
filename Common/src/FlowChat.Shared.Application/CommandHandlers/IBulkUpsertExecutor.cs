using FlowChat.Core.Results;

namespace FlowChat.Shared.Application;

public interface IBulkUpsertExecutor<TItem>
    where TItem : notnull
{
    Task<FlowChatResult<BulkUpsertCommandResult>> UpsertAsync(
        IReadOnlyCollection<TItem> items,
        CancellationToken cancellationToken);
}
