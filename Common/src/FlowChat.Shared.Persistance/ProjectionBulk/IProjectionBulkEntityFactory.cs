using FlowChat.Shared.Application;

namespace FlowChat.Shared.Persistance.ProjectionBulk;

public interface IProjectionBulkEntityFactory<TItem, TValue, TEntity>
    where TItem : notnull, IProjectionCommandItem<TValue>
    where TValue : class
    where TEntity : ReadModelEntityBase
{
    IReadOnlyList<string> UpdateByProperties { get; }

    TEntity CreateUpsertEntity(
        TValue value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc);

    TEntity CreateTombstoneEntity(
        TItem item,
        DateTimeOffset now);
}
