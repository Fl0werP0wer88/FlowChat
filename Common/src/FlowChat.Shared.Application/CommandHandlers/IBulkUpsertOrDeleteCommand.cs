namespace FlowChat.Shared.Application;

public interface IBulkUpsertOrDeleteCommand<TValue> : ICommand<BulkUpsertOrDeleteCommandResult>
    where TValue : class
{
    IReadOnlyCollection<BulkCommandItem<TValue>> Items { get; }
}
