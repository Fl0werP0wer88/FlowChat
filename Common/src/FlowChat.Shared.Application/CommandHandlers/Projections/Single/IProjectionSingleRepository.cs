namespace FlowChat.Shared.Application;

public interface IProjectionSingleRepository<TValue>
    where TValue : class
{
    Task UpsertOrSoftDeleteAsync(
        ProjectionCommandItem<TValue> item,
        CancellationToken cancellationToken);
}
