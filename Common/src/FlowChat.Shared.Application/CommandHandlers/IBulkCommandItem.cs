using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IBulkCommandItem<TValue>
    where TValue : class
{
    Id<TValue> EntityId { get; }
    TValue? Value { get; }
}
