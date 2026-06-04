using EFCore.BulkExtensions;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Persistence.Entities;

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
        var entities = items.Select(item => CreateEntity(item, now)).ToList();

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
                PropertiesToExcludeOnUpdate =
                [
                    nameof(ContactObserverReadModelEntity.CreatedBy),
                    nameof(ContactObserverReadModelEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);
    }

    private static ContactObserverReadModelEntity CreateEntity(
        UserContactProjectionCommandItem item,
        DateTimeOffset now) =>
        item.Value is null
            ? CreateTombstoneEntity(item, now)
            : CreateUpsertEntity(item.Value, item.SourceVersion, now);

    private static ContactObserverReadModelEntity CreateUpsertEntity(
        ContactObserverProjectionDto item,
        int sourceVersion,
        DateTimeOffset now) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = sourceVersion,
            DeletedAt = null,
            CreatedBy = item.Source,
            CreatedAtUtc = now,
            LastModifiedBy = item.Source,
            LastModifiedAtUtc = now
        };

    private static ContactObserverReadModelEntity CreateTombstoneEntity(
        UserContactProjectionCommandItem item,
        DateTimeOffset now) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = item.SourceVersion,
            DeletedAt = now,
            CreatedBy = TombstoneSource,
            CreatedAtUtc = now,
            LastModifiedBy = TombstoneSource,
            LastModifiedAtUtc = now
        };
}
