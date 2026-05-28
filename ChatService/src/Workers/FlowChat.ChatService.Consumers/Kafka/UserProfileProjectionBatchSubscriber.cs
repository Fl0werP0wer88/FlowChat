using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Messaging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileProjectionBatchSubscriber(
    IChatInternalApiClient apiClient,
    IMapper mapper,
    ILogger<UserProfileProjectionBatchSubscriber> logger)
{
    [Subscribe]
    public async Task HandleAsync(
        IAsyncEnumerable<IntegrationEvent> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<BulkUpsertOrDeleteUserProfileProjectionRequestItem>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            var item = UserProfileSubscriberHelper.MapAndFilterEvents(message, mapper);
            if (item is null)
            {
                logger.LogDebug(
                    "Skipping unsupported user profile projection event {EventType}.",
                    message.GetType().Name);
                continue;
            }

            items.Add(item);
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty user profile projection batch.");
            return;
        }

        await apiClient.BulkUpsertOrDeleteUserProfileProjectionAsync(
            UserProfileSubscriberHelper.CreateBulkUpsertOrDeleteRequest(items),
            cancellationToken);

        logger.LogInformation(
            "Processed {Count} user profile projection events from Kafka batch.",
            items.Count);
    }
}
