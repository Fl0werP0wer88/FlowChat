namespace FlowChat.Shared.Application;

public interface IProjectionBulkRepository<TItem>
    where TItem : notnull
{
    Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<TItem> items,
        CancellationToken cancellationToken);
}
