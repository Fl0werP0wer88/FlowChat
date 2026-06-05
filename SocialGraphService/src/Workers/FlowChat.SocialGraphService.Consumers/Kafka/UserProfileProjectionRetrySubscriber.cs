using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.Services;
using Silverback.Messaging.Subscribers;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileProjectionRetrySubscriber(
    ISocialGraphInternalApiClient apiClient,
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
