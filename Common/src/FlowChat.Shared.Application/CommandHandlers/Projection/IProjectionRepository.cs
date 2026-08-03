namespace FlowChat.Shared.Application;

public interface IProjectionRepository<TValue>
    where TValue : class
{
    Task UpsertOrSoftDeleteAsync(
        ProjectionCommandItem<TValue> item,
        CancellationToken cancellationToken);
}
