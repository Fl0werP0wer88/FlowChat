using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.ProjectionBulk;

public sealed class ProjectionBulkRepository<TDbContext, TItem, TValue, TEntity, TEntityFactory>(
    TDbContext dbContext,
    TEntityFactory entityFactory)
    : ProjectionBulkRepositoryBase<TDbContext, TItem, TValue, TEntity>(dbContext),
        IProjectionBulkRepository<TItem>
    where TDbContext : DbContext
    where TItem : notnull, IProjectionCommandItem<TValue>
    where TValue : class
    where TEntity : ReadModelEntityBase
    where TEntityFactory : IProjectionBulkEntityFactory<TItem, TValue, TEntity>
{
    public Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<TItem> items,
        CancellationToken cancellationToken) =>
        BulkUpsertProjectionAsync(
            items,
            entityFactory.UpdateByProperties,
            cancellationToken);

    protected override TEntity CreateUpsertEntity(
        TValue value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        entityFactory.CreateUpsertEntity(
            value,
            sourceVersion,
            sourceCreatedAtUtc,
            sourceLastModifiedAtUtc,
            sourceDeletedAtUtc);

    protected override TEntity CreateTombstoneEntity(
        TItem item,
        DateTimeOffset now) =>
        entityFactory.CreateTombstoneEntity(item, now);
}
