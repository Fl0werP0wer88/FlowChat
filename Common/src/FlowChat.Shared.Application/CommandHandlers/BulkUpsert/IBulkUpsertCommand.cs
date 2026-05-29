namespace FlowChat.Shared.Application;

public interface IBulkUpsertCommand<TItem> : ICommand<BulkUpsertCommandResult>
    where TItem : notnull
{
    IReadOnlyCollection<TItem> Items { get; }
}
