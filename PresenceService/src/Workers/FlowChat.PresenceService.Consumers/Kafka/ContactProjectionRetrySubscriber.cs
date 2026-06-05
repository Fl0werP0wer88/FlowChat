using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Services;
using Silverback.Messaging.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactProjectionRetrySubscriber(
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<ContactProjectionRetrySubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ContactRetryConsumerName)]
    public async Task HandleAsync(
        ProjectionIntegrationEvent<ContactReadModel> message,
        CancellationToken cancellationToken)
    {
        var item = ContactProjectionSubscriberHelper.MapProjectionEvent(message);

        await presenceInternalApiClient.BulkUpsertOrDeleteUserContactProjectionAsync(
            ContactProjectionSubscriberHelper.CreateBulkUpsertOrDeleteRequest([item]),
            cancellationToken);

        logger.LogInformation(
            "Processed contact projection event {ObservedUserId}/{ObserverUserId} from Kafka retry topic.",
            item.ObservedUserId,
            item.ObserverUserId);
    }
}
