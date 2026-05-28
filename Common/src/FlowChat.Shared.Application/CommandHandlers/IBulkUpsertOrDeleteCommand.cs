namespace FlowChat.Shared.Application;

public interface IBulkUpsertOrDeleteCommand<TItem, TValue> : ICommand<BulkUpsertOrDeleteCommandResult>
    where TItem : IBulkCommandItem<TValue>
    where TValue : class
{
    IReadOnlyCollection<TItem> Items { get; }
}
