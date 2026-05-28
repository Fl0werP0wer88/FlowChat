namespace FlowChat.Shared.Application;

public interface IBulkUpsertOrDeleteCommand<TItem> : ICommand<BulkUpsertOrDeleteCommandResult>
    where TItem : IBulkCommandItem
{
    IReadOnlyCollection<TItem> Items { get; }
}
