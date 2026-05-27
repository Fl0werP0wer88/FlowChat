using FlowChat.PresenceService.Consumers.Presence.Contracts;

namespace FlowChat.PresenceService.Consumers.Services;

public interface IPresenceInternalApiClient
{
    Task BulkUpsertContactObserverProjectionAsync(
        BulkUpsertContactObserverProjectionRequest request,
        CancellationToken cancellationToken);

    Task DeleteContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken);
}
