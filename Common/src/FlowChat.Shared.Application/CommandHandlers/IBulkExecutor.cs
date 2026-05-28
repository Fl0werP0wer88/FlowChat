using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IBulkExecutor<TValue>
{
    Task<FlowChatResult<int>> UpsertAsync(IReadOnlyCollection<TValue> items, CancellationToken cancellationToken);
    Task<FlowChatResult<int>> DeleteAsync(IReadOnlyCollection<Id<TValue>> ids, CancellationToken cancellationToken);
}
