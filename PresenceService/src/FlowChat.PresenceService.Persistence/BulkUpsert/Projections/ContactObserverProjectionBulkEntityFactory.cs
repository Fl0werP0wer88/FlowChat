using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.ProjectionBulk;

namespace FlowChat.PresenceService.Persistence.BulkUpsert.Projections;

public sealed class ContactObserverProjectionBulkEntityFactory
    : IProjectionBulkEntityFactory<ContactObserverProjectionDto, ContactObserverReadModelEntity>
{
    public IReadOnlyList<string> UpdateByProperties { get; } =
    [
        nameof(ContactObserverReadModelEntity.ObservedUserId),
        nameof(ContactObserverReadModelEntity.ObserverUserId)
    ];

    public ContactObserverReadModelEntity CreateUpsertEntity(
        ContactObserverProjectionDto value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        new()
        {
            ObservedUserId = value.ObservedUserId,
            ObserverUserId = value.ObserverUserId,
            SourceVersion = sourceVersion,
            SourceCreatedAtUtc = sourceCreatedAtUtc,
            SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
            SourceDeletedAtUtc = sourceDeletedAtUtc
        };

    public ContactObserverReadModelEntity CreateTombstoneEntity(
        ProjectionCommandItem<ContactObserverProjectionDto> item,
        DateTimeOffset now) =>
        new()
        {
            ObservedUserId = item.Value.ObservedUserId,
            ObserverUserId = item.Value.ObserverUserId,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
        };
}
