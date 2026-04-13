using FlowChat.PresenceService.Consumers.Presence.Contracts;

namespace FlowChat.PresenceService.Consumers.Services;

public interface IPresenceInternalApiClient
{
    Task InitializePresenceStatusAsync(
        PresenceStatusRequest request,
        CancellationToken cancellationToken);

    Task DeletePresenceStatusAsync(
        PresenceStatusRequest request,
        CancellationToken cancellationToken);

    Task InsertContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken);

    Task DeleteContactObserverProjectionAsync(
        ContactObserverProjectionRequest request,
        CancellationToken cancellationToken);
}
