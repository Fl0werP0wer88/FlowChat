using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IBulkRepository<TApplicationObject>
{
    Task<FlowChatResult<int>> BulkUpsertAsync(IReadOnlyCollection<TApplicationObject> items, CancellationToken cancellationToken);
    Task<FlowChatResult<int>> BulkDeleteAsync(IReadOnlyCollection<Id<TApplicationObject>> ids, CancellationToken cancellationToken);
}
