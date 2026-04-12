using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.PresenceService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactAddedSubscriber(
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<ContactAddedSubscriber> logger)
    : SubscriberBase<ContactAddedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        ContactAddedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        ContactProjectionSubscriberHelper.InsertAsync(
            presenceInternalApiClient,
            Logger,
            ContactProjectionSubscriberHelper.Map(message.OwnerUserId, message.ContactUserId),
            nameof(ContactAddedIntegrationEvent),
            cancellationToken);
}
