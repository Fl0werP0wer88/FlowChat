using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.SocialGraphService.Consumers.Services;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileCreatedSubscriber(
    ISocialGraphInternalApiClient socialGraphInternalApiClient,
    ILogger<UserProfileCreatedSubscriber> logger)
    : SubscriberBase<UserProfileCreatedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        UserProfileCreatedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UserProfileSubscriberHelper.InsertAsync(
            socialGraphInternalApiClient,
            Logger,
            UserProfileSubscriberHelper.Map(message),
            nameof(UserProfileCreatedIntegrationEvent),
            cancellationToken);
}
