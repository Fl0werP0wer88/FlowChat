using FlowChat.Shared.Application;

namespace FlowChat.Shared.Persistance.ProjectionBulk;

public interface IProjectionBulkEntityFactory<TValue, TEntity>
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
        ProjectionCommandItem<TValue> item,
        DateTimeOffset now);
}
