using System.Data.Common;
using EFCore.BulkExtensions;
using FlowChat.Core.Exceptions;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.ProjectionBulk;

public abstract class ProjectionBulkRepositoryBase<TDbContext, TItem, TValue, TEntity>(TDbContext dbContext)
    where TDbContext : DbContext
    where TItem : IProjectionCommandItem<TValue>
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

        try
        {
            await dbContext.BulkInsertOrUpdateAsync(
                entities,
                new BulkConfig
                {
                    // flowchat_app has CRUD-only access; regular helper tables require CREATE on the public schema
                    UseTempDB = true,
                    UpdateByProperties = updateByProperties.ToList(),
                    OnConflictUpdateWhereSql = (existing, inserted) =>
                        $"{inserted}.\"{nameof(ReadModelEntityBase.SourceVersion)}\" > {existing}.\"{nameof(ReadModelEntityBase.SourceVersion)}\""
                },
                cancellationToken: cancellationToken);
        }
        // BulkInsertOrUpdateAsync executes raw provider SQL and surfaces the provider's DbException directly
        // (not wrapped in DbUpdateException); a non-transient failure here likely means a single item in the
        // batch carries invalid data, so surface it as isolable to let the batch be retried item-by-item.
        catch (DbException exception) when (!exception.IsTransient)
        {
            throw new IsolableException(
                "Bulk projection upsert failed; one or more items may contain invalid data.",
                exception);
        }
    }

    protected abstract TEntity CreateUpsertEntity(
        TValue item,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc);

    protected abstract TEntity CreateTombstoneEntity(
        TItem item,
        DateTimeOffset now);

    private TEntity CreateEntity(
        TItem item,
        DateTimeOffset now)
    =>
        item.Operation == FlowChat.Core.Messaging.OperationType.Deleted
            ? CreateTombstoneEntity(item, now)
            : CreateUpsertEntity(
                item.Value,
                item.SourceVersion,
                item.SourceCreatedAtUtc,
                item.SourceLastModifiedAtUtc,
                item.SourceDeletedAtUtc);
}
