using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using Silverback.Messaging.Subscribers;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileProjectionRetrySubscriber(
    IChatInternalApiClient apiClient,
    IMapper mapper,
    ILogger<UserProfileProjectionRetrySubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.UserProfileRetryConsumerName)]
    public async Task HandleAsync(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        CancellationToken cancellationToken)
    {
        var item = UserProfileSubscriberHelper.MapProjectionEvent(message, mapper);

        await apiClient.BulkUpsertOrDeleteUserProfileProjectionAsync(
            UserProfileSubscriberHelper.CreateBulkUpsertOrDeleteRequest([item]),
            cancellationToken);

        logger.LogInformation(
            "Processed user profile projection event {UserProfileId} from Kafka retry topic.",
            item.UserProfileId);
    }
}
