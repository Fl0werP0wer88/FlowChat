using EFCore.BulkExtensions;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkRepository(AppDbContext dbContext)
    : IContactObserverProjectionBulkRepository
{
    private const string TombstoneSource = "social-graph-contact-events";

    public async Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserContactProjectionCommandItem> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var existingEntities = await GetExistingEntitiesAsync(items, cancellationToken);
        var entities = items
            .Select(item => CreateEntity(
                item,
                now,
                existingEntities.GetValueOrDefault((item.ObservedUserId, item.ObserverUserId))))
            .ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                // flowchat_app has CRUD-only access; regular helper tables require CREATE on the public schema
                UseTempDB = true,
                UpdateByProperties =
                [
                    nameof(ContactObserverReadModelEntity.ObservedUserId),
                    nameof(ContactObserverReadModelEntity.ObserverUserId)
                ],
                OnConflictUpdateWhereSql = (existing, inserted) =>
                    $"{inserted}.\"SourceVersion\" > {existing}.\"SourceVersion\"",
                PropertiesToIncludeOnUpdate =
                [
                    nameof(ContactObserverReadModelEntity.SourceVersion),
                    nameof(ContactObserverReadModelEntity.DeletedAt),
                    nameof(ContactObserverReadModelEntity.LastModifiedBy),
                    nameof(ContactObserverReadModelEntity.LastModifiedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);
    }

    private static ContactObserverReadModelEntity CreateEntity(
        UserContactProjectionCommandItem item,
        DateTimeOffset now,
        ContactObserverReadModelEntity? existingEntity) =>
        item.Value is null
            ? CreateTombstoneEntity(item, now, existingEntity)
            : CreateUpsertEntity(item.Value, item.SourceVersion, now, existingEntity);

    private static ContactObserverReadModelEntity CreateUpsertEntity(
        ContactObserverProjectionDto item,
        int sourceVersion,
        DateTimeOffset now,
        ContactObserverReadModelEntity? existingEntity) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = sourceVersion,
            DeletedAt = null,
            CreatedBy = existingEntity?.CreatedBy ?? item.Source,
            CreatedAtUtc = existingEntity?.CreatedAtUtc ?? now,
            LastModifiedBy = item.Source,
            LastModifiedAtUtc = now
        };

    private static ContactObserverReadModelEntity CreateTombstoneEntity(
        UserContactProjectionCommandItem item,
        DateTimeOffset now,
        ContactObserverReadModelEntity? existingEntity) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = item.SourceVersion,
            DeletedAt = now,
            CreatedBy = existingEntity?.CreatedBy ?? TombstoneSource,
            CreatedAtUtc = existingEntity?.CreatedAtUtc ?? now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };

    private async Task<Dictionary<(Guid ObservedUserId, Guid ObserverUserId), ContactObserverReadModelEntity>> GetExistingEntitiesAsync(
        IReadOnlyCollection<UserContactProjectionCommandItem> items,
        CancellationToken cancellationToken)
    {
        var observedUserIds = items.Select(item => item.ObservedUserId).Distinct().ToArray();
        var observerUserIds = items.Select(item => item.ObserverUserId).Distinct().ToArray();

        var candidates = await dbContext.ContactObserverProjections
            .AsNoTracking()
            .Where(entity => observedUserIds.Contains(entity.ObservedUserId) &&
                             observerUserIds.Contains(entity.ObserverUserId))
            .ToListAsync(cancellationToken);

        return candidates.ToDictionary(
            entity => (entity.ObservedUserId, entity.ObserverUserId),
            entity => entity);
    }
}
