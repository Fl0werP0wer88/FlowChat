using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.PresenceService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactDeletedSubscriber(
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<ContactDeletedSubscriber> logger)
    : SubscriberBase<ContactDeletedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        ContactDeletedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        ContactProjectionSubscriberHelper.DeleteAsync(
            presenceInternalApiClient,
            Logger,
            ContactProjectionSubscriberHelper.Map(message.OwnerUserId, message.ContactUserId),
            nameof(ContactDeletedIntegrationEvent),
            cancellationToken);
}
