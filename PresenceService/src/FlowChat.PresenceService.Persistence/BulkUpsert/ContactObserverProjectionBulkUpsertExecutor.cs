using EFCore.BulkExtensions;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Application;

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

        var entities = items.Select(item => new ContactObserverReadModelEntity
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            CreatedBy = ProjectionSource,
            CreatedAtUtc = item.CreatedAtUtc,
            LastModifiedBy = ProjectionSource,
            LastModifiedAtUtc = item.LastModifiedAtUtc
        }).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                // flowchat_app has CRUD-only access; regular helper tables require CREATE on the public schema
                UseTempDB = true,
                PropertiesToExcludeOnUpdate =
                [
                    nameof(ContactObserverReadModelEntity.CreatedBy),
                    nameof(ContactObserverReadModelEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
