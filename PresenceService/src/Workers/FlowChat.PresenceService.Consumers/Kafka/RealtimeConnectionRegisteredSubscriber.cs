using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class RealtimeConnectionRegisteredSubscriber(
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<RealtimeConnectionRegisteredSubscriber> logger)
    : SubscriberBase<RealtimeConnectionRegisteredIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        RealtimeConnectionRegisteredIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        Validate(message);
        if (!message.IsFirstConnectionForUser)
        {
            return;
        }

        await presenceInternalApiClient.InitializePresenceStatusAsync(
            new PresenceStatusRequest { UserId = message.UserId },
            cancellationToken);
    }

    private static void Validate(RealtimeConnectionRegisteredIntegrationEvent message)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.ConnectionId))
        {
            throw new NonTransientException("Payload does not contain valid ConnectionId.");
        }

        if (message.ActiveConnectionCount <= 0)
        {
            throw new NonTransientException("Payload does not contain valid ActiveConnectionCount.");
        }
    }
}
