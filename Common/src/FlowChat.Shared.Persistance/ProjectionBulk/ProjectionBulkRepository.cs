using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.ProjectionBulk;

public sealed class ProjectionBulkRepository<TDbContext, TValue, TEntity, TEntityFactory>(
    TDbContext dbContext,
    TEntityFactory entityFactory)
    : ProjectionBulkRepositoryBase<TDbContext, ProjectionCommandItem<TValue>, TValue, TEntity>(dbContext),
        IProjectionBulkRepository<ProjectionCommandItem<TValue>>
    where TDbContext : DbContext
    where TValue : class
    where TEntity : ReadModelEntityBase
    where TEntityFactory : IProjectionBulkEntityFactory<TValue, TEntity>
{
    public Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<ProjectionCommandItem<TValue>> items,
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
        ProjectionCommandItem<TValue> item,
        DateTimeOffset now) =>
        entityFactory.CreateTombstoneEntity(item, now);
}
