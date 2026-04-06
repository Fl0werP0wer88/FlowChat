using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.SocialGraphService.Consumers.Services;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileStateChangedSubscriber(
    ISocialGraphInternalApiClient socialGraphInternalApiClient,
    ILogger<UserProfileStateChangedSubscriber> logger)
    : SubscriberBase<UserProfileStateChangedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        UserProfileStateChangedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UserProfileSubscriberHelper.UpdateAsync(
            socialGraphInternalApiClient,
            Logger,
            UserProfileSubscriberHelper.Map(message),
            nameof(UserProfileStateChangedIntegrationEvent),
            cancellationToken);
}
