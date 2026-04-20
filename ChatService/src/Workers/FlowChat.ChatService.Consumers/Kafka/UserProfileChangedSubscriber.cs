using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.ChatService.Consumers.Kafka;

public sealed class UserProfileChangedSubscriber(
    IChatInternalApiClient apiClient,
    ILogger<UserProfileChangedSubscriber> logger)
    : SubscriberBase<UserProfileChangedIntegrationEvent>(logger)
{
    protected override Task ExecuteAsync(
        UserProfileChangedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UserProfileSubscriberHelper.UpdateAsync(
            apiClient,
            Logger,
            UserProfileSubscriberHelper.Map(message),
            nameof(UserProfileChangedIntegrationEvent),
            cancellationToken);
}
