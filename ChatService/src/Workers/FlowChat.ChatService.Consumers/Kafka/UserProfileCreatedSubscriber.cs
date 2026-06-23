using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileCreatedSubscriber(
    IChatInternalApiClient apiClient,
    ILogger<UserProfileCreatedSubscriber> logger)
    : SubscriberBase<UserProfileCreatedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        UserProfileCreatedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UserProfileSubscriberHelper.InsertAsync(
            apiClient,
            Logger,
            UserProfileSubscriberHelper.Map(message),
            nameof(UserProfileCreatedIntegrationEvent),
            cancellationToken);
}
