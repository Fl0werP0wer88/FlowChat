using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;

namespace FlowChat.PresenceService.Consumers.Kafka;

internal static class ContactProjectionSubscriberHelper
{
    public static ContactObserverProjectionRequest Map(Guid ownerUserId, Guid contactUserId) =>
        new()
        {
            ObservedUserId = contactUserId,
            ObserverUserId = ownerUserId
        };

    public static async Task InsertAsync(
        IPresenceInternalApiClient presenceInternalApiClient,
        ILogger logger,
        ContactObserverProjectionRequest request,
        string eventName,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Applying {EventName} to PresenceService for observed user {ObservedUserId} and observer {ObserverUserId}.",
            eventName,
            request.ObservedUserId,
            request.ObserverUserId);

        await presenceInternalApiClient.BulkUpsertContactObserverProjectionAsync(
            new BulkUpsertContactObserverProjectionRequest { Items = [request] },
            cancellationToken);
    }

    public static async Task DeleteAsync(
        IPresenceInternalApiClient presenceInternalApiClient,
        ILogger logger,
        ContactObserverProjectionRequest request,
        string eventName,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Applying {EventName} to PresenceService for observed user {ObservedUserId} and observer {ObserverUserId}.",
            eventName,
            request.ObservedUserId,
            request.ObserverUserId);

        await presenceInternalApiClient.DeleteContactObserverProjectionAsync(request, cancellationToken);
    }
}
