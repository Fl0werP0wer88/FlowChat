using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.RealtimeService.Worker.Realtime.Contracts;
using FlowChat.RealtimeService.Worker.Services;
using Silverback.Messaging.Subscribers;

namespace FlowChat.RealtimeService.Worker.Kafka;

public sealed class UserPresenceChangedSubscriber(
    IRealtimeInternalApiClient realtimeInternalApiClient,
    ILogger<UserPresenceChangedSubscriber> logger)
{
    private static readonly HashSet<string> AllowedStatuses =
        ["online", "away", "offline"];

    [Subscribe]
    public async Task HandleAsync(UserPresenceChangedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var normalizedStatus = ValidateAndNormalizeStatus(message);

        try
        {
            var request = new PublishPresenceChangeRequest
            {
                UserId = message.UserId,
                Status = normalizedStatus,
                ChangedAtUtc = message.ChangedAtUtc,
                RecipientUserIds = message.RecipientUserIds
                    .Where(userId => userId != Guid.Empty)
                    .Distinct()
                    .ToArray()
            };

            await realtimeInternalApiClient.PublishPresenceChangeAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to forward {EventType} event for user {UserId} to RealtimeService API.",
                nameof(UserPresenceChangedIntegrationEvent),
                message.UserId);

            throw;
        }
    }

    private static string ValidateAndNormalizeStatus(UserPresenceChangedIntegrationEvent message)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.Status))
        {
            throw new InvalidOperationException("Payload does not contain valid Status.");
        }

        if (message.RecipientUserIds is null || !message.RecipientUserIds.Any(userId => userId != Guid.Empty))
        {
            throw new InvalidOperationException("Payload does not contain valid RecipientUserIds.");
        }

        var normalizedStatus = message.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            throw new InvalidOperationException("Payload contains unsupported Status.");
        }

        return normalizedStatus;
    }
}
