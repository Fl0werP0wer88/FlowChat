using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class UserPresenceChangedSubscriber(
    IRealtimeInternalApiClient realtimeInternalApiClient,
    ILogger<UserPresenceChangedSubscriber> logger)
    : SubscriberBase<UserStatusChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        UserStatusChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        Validate(message);

        var request = new PublishPresenceChangeRequest
        {
            UserId = message.UserId,
            Status = message.Status,
            ChangedAtUtc = message.ChangedAtUtc,
            RecipientUserIds = message.RecipientUserIds
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray()
        };

        await realtimeInternalApiClient.PublishPresenceChangeAsync(request, cancellationToken);
    }

    private static void Validate(UserStatusChangedIntegrationEvent message)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        if (!Enum.IsDefined(typeof(FlowChat.Core.Domain.UserStatus), message.Status))
        {
            throw new NonTransientException("Payload does not contain valid Status.");
        }

        if (message.RecipientUserIds is null || !message.RecipientUserIds.Any(userId => userId != Guid.Empty))
        {
            throw new NonTransientException("Payload does not contain valid RecipientUserIds.");
        }
    }
}
