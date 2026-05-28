using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IBulkRepository<TValue>
{
    Task<FlowChatResult<int>> BulkUpsertAsync(IReadOnlyCollection<TValue> items, CancellationToken cancellationToken);
    Task<FlowChatResult<int>> BulkDeleteAsync(IReadOnlyCollection<Id<TValue>> ids, CancellationToken cancellationToken);
}
