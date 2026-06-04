using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Persistance.BulkUpsert;

namespace FlowChat.PresenceService.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkRepository(AppDbContext dbContext)
    : ProjectionBulkRepositoryBase<AppDbContext, UserContactProjectionCommandItem, ContactObserverProjectionDto, ContactObserverReadModelEntity>(dbContext),
        IContactObserverProjectionBulkRepository
{
    private const string TombstoneSource = "social-graph-contact-events";

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

    protected override ContactObserverProjectionDto? GetValue(UserContactProjectionCommandItem item) =>
        item.Value;

    protected override int GetSourceVersion(UserContactProjectionCommandItem item) =>
        item.SourceVersion;

    protected override ContactObserverReadModelEntity CreateUpsertEntity(
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

    protected override ContactObserverReadModelEntity CreateTombstoneEntity(
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
