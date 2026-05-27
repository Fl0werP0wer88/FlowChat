using EFCore.BulkExtensions;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.BulkUpsert;

public sealed class ContactObserverProjectionBulkUpsertExecutor(AppDbContext dbContext)
    : IBulkUpsertExecutor<ContactObserverProjectionDto>
{
    private const string ProjectionSource = "social-graph-contact-events";

    public async Task<FlowChatResult<BulkUpsertCommandResult>> UpsertAsync(
        IReadOnlyCollection<ContactObserverProjectionDto> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.Empty);
        }

        var observedUserIds = items.Select(item => item.ObservedUserId).Distinct().ToArray();
        var existingCreationAudit = await dbContext.ContactObserverProjections
            .Where(entity => observedUserIds.Contains(entity.ObservedUserId))
            .Select(entity => new
            {
                entity.ObservedUserId,
                entity.ObserverUserId,
                entity.CreatedBy,
                entity.CreatedAtUtc
            })
            .ToDictionaryAsync(
                entity => (entity.ObservedUserId, entity.ObserverUserId),
                cancellationToken);

        var entities = items.Select(item => new ContactObserverProjectionEntity
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            CreatedBy = existingCreationAudit.TryGetValue((item.ObservedUserId, item.ObserverUserId), out var audit)
                ? audit.CreatedBy
                : ProjectionSource,
            CreatedAtUtc = existingCreationAudit.TryGetValue((item.ObservedUserId, item.ObserverUserId), out audit)
                ? audit.CreatedAtUtc
                : item.CreatedAtUtc,
            LastModifiedBy = ProjectionSource,
            LastModifiedAtUtc = item.LastModifiedAtUtc
        }).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                PropertiesToExcludeOnUpdate =
                [
                    nameof(ContactObserverProjectionEntity.CreatedBy),
                    nameof(ContactObserverProjectionEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
