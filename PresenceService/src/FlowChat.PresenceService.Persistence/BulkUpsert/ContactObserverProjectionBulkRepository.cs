using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Persistance.ProjectionBulk;

namespace FlowChat.PresenceService.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkRepository(AppDbContext dbContext)
    : ProjectionBulkRepositoryBase<AppDbContext, UserContactProjectionCommandItem, ContactObserverProjectionDto, ContactObserverReadModelEntity>(dbContext),
        IContactObserverProjectionBulkRepository
{
    public Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserContactProjectionCommandItem> items,
        CancellationToken cancellationToken) =>
        BulkUpsertProjectionAsync(
            items,
            [
                nameof(ContactObserverReadModelEntity.ObservedUserId),
                nameof(ContactObserverReadModelEntity.ObserverUserId)
            ],
            cancellationToken);

    protected override ContactObserverReadModelEntity CreateUpsertEntity(
        ContactObserverProjectionDto item,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = sourceVersion,
            SourceCreatedAtUtc = sourceCreatedAtUtc,
            SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
            SourceDeletedAtUtc = sourceDeletedAtUtc
        };

    protected override ContactObserverReadModelEntity CreateTombstoneEntity(
        UserContactProjectionCommandItem item,
        DateTimeOffset now) =>
        new()
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
        };
}
