using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IContactObserverProjectionBulkRepository
{
    Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<UserContactProjectionCommandItem> items,
        CancellationToken cancellationToken);
}
