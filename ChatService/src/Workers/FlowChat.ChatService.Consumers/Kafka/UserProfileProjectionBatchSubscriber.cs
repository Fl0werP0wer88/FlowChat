using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using Silverback.Messaging.Subscribers;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileProjectionBatchSubscriber(
    IChatInternalApiClient apiClient,
    IMapper mapper,
    ILogger<UserProfileProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.UserProfileMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<UserProfileReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<BulkUpsertOrDeleteUserProfileProjectionRequestItem>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            items.Add(UserProfileSubscriberHelper.MapProjectionEvent(message, mapper));
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
