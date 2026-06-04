using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
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
        var pendingUpserts = new List<ProjectionIntegrationEvent<ContactReadModel>>();
        var processedCount = 0;

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            processedCount++;

            switch (message.Operation)
            {
                case OperationType.Created:
                case OperationType.Updated:
                    pendingUpserts.Add(message);
                    break;
                case OperationType.Deleted:
                    await ContactProjectionSubscriberHelper.FlushUpsertsAsync(
                        presenceInternalApiClient,
                        logger,
                        pendingUpserts,
                        cancellationToken);

                    await ContactProjectionSubscriberHelper.DeleteAsync(
                        presenceInternalApiClient,
                        logger,
                        message,
                        cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported contact projection operation {message.Operation}.");
            }
        }

        await ContactProjectionSubscriberHelper.FlushUpsertsAsync(
            presenceInternalApiClient,
            logger,
            pendingUpserts,
            cancellationToken);

        if (processedCount == 0)
        {
            logger.LogDebug("Skipping empty contact projection batch.");
            return;
        }

        logger.LogInformation(
            "Processed {Count} contact projection events from Kafka batch.",
            processedCount);
    }
}
