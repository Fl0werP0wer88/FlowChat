using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using Silverback.Messaging.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactProjectionBatchSubscriber(
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<ContactProjectionBatchSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ContactReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<BulkUpsertOrDeleteUserContactProjectionRequestItem>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            items.Add(ContactProjectionSubscriberHelper.MapProjectionEvent(message));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty contact projection batch.");
            return;
        }

        await presenceInternalApiClient.BulkUpsertOrDeleteUserContactProjectionAsync(
            ContactProjectionSubscriberHelper.CreateBulkUpsertOrDeleteRequest(items),
            cancellationToken);

        logger.LogInformation(
            "Processed {Count} contact projection events from Kafka batch.",
            items.Count);
    }
}
