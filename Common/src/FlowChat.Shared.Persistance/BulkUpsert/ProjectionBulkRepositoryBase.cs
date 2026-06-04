using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.BulkUpsert;

public abstract class ProjectionBulkRepositoryBase<TDbContext, TItem, TValue, TEntity>(TDbContext dbContext)
    where TDbContext : DbContext
    where TItem : notnull
    where TValue : class
    where TEntity : ReadModelEntityBase
{
    protected async Task BulkUpsertProjectionAsync(
        IReadOnlyCollection<TItem> items,
        IReadOnlyList<string> updateByProperties,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(updateByProperties);
        cancellationToken.ThrowIfCancellationRequested();

        if (items.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var entities = items.Select(item => CreateEntity(item, now)).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                // flowchat_app has CRUD-only access; regular helper tables require CREATE on the public schema
                UseTempDB = true,
                UpdateByProperties = updateByProperties.ToList(),
                OnConflictUpdateWhereSql = (existing, inserted) =>
                    $"{inserted}.\"{nameof(ReadModelEntityBase.SourceVersion)}\" > {existing}.\"{nameof(ReadModelEntityBase.SourceVersion)}\"",
                PropertiesToExcludeOnUpdate =
                [
                    nameof(AuditableReadEntityBase.CreatedBy),
                    nameof(AuditableReadEntityBase.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);
    }

    protected abstract TValue? GetValue(TItem item);

    protected abstract int GetSourceVersion(TItem item);

    protected abstract TEntity CreateUpsertEntity(
        TValue item,
        int sourceVersion,
        DateTimeOffset now);

    protected abstract TEntity CreateTombstoneEntity(
        TItem item,
        DateTimeOffset now);

    private TEntity CreateEntity(
        TItem item,
        DateTimeOffset now)
    {
        var value = GetValue(item);

        return value is null
            ? CreateTombstoneEntity(item, now)
            : CreateUpsertEntity(value, GetSourceVersion(item), now);
    }
}
