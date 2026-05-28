using FlowChat.Core.Results;

namespace FlowChat.Shared.Application;

public interface IBulkExecutor<TItem>
    where TItem : notnull
{
    Task<FlowChatResult<int>> UpsertAsync(IReadOnlyCollection<TItem> items, CancellationToken cancellationToken);
    Task<FlowChatResult<int>> DeleteAsync(IReadOnlyCollection<TItem> items, CancellationToken cancellationToken);
}
