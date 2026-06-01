using MediatR;

namespace FlowChat.Shared.Application;

public interface IBulkUpsertOrDeleteCommand<TValue> : ICommand<Unit>
    where TValue : class
{
    IReadOnlyCollection<BulkCommandItem<TValue>> Items { get; }
}
