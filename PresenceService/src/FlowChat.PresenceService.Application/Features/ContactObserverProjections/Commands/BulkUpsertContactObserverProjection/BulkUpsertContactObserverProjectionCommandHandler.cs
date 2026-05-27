using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;

public sealed class BulkUpsertContactObserverProjectionCommandHandler(
    IUnitOfWork unitOfWork,
    IBulkUpsertExecutor<ContactObserverProjectionDto> bulkUpsertExecutor)
    : BulkUpsertCommandHandlerBase<
        BulkUpsertContactObserverProjectionCommand,
        BulkUpsertContactObserverProjectionCommandItem,
        ContactObserverProjectionDto>(unitOfWork, bulkUpsertExecutor)
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    protected override ContactObserverProjectionDto MapItem(BulkUpsertContactObserverProjectionCommandItem item)
    {
        return new ContactObserverProjectionDto
        {
            ObservedUserId = item.ObservedUserId,
            ObserverUserId = item.ObserverUserId,
            CreatedAtUtc = _now,
            LastModifiedAtUtc = _now
        };
    }
}
