using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;

namespace FlowChat.PresenceService.Consumers.Kafka;

internal static class ContactProjectionSubscriberHelper
{
    public static ContactObserverProjectionRequest Map(ContactReadModel value) =>
        new()
        {
            ObservedUserId = value.ContactUserId,
            ObserverUserId = value.OwnerUserId
        };

    public static async Task FlushUpsertsAsync(
        IPresenceInternalApiClient presenceInternalApiClient,
        ILogger logger,
        List<ProjectionIntegrationEvent<ContactReadModel>> pendingUpserts,
        CancellationToken cancellationToken)
    {
        if (pendingUpserts.Count == 0)
        {
            return;
        }

        var request = new BulkUpsertContactObserverProjectionRequest
        {
            Items = pendingUpserts
                .Select(message => Map(message.Value))
                .ToArray()
        };

        await presenceInternalApiClient.BulkUpsertContactObserverProjectionAsync(request, cancellationToken);

        logger.LogInformation(
            "Applied {Count} contact projection upserts to PresenceService.",
            request.Items.Count);

        pendingUpserts.Clear();
    }

    public static async Task DeleteAsync(
        IPresenceInternalApiClient presenceInternalApiClient,
        ILogger logger,
        ProjectionIntegrationEvent<ContactReadModel> message,
        CancellationToken cancellationToken)
    {
        var request = Map(message.Value);

        logger.LogInformation(
            "Applying contact projection delete to PresenceService for observed user {ObservedUserId} and observer {ObserverUserId}.",
            request.ObservedUserId,
            request.ObserverUserId);

        await presenceInternalApiClient.DeleteContactObserverProjectionAsync(request, cancellationToken);
    }
}
