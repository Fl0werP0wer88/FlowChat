using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class UserPresenceChangedSubscriber(
    IRealtimeInternalApiClient realtimeInternalApiClient,
    ILogger<UserPresenceChangedSubscriber> logger)
    : SubscriberBase<UserPresenceChangedIntegrationEvent>(logger)
{
    private static readonly HashSet<string> AllowedStatuses =
        ["online", "away", "offline"];

    protected override async Task ExecuteAsync(
        UserPresenceChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var normalizedStatus = ValidateAndNormalizeStatus(message);

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

    private static string ValidateAndNormalizeStatus(UserPresenceChangedIntegrationEvent message)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(message.Status))
        {
            throw new NonTransientException("Payload does not contain valid Status.");
        }

        if (message.RecipientUserIds is null || !message.RecipientUserIds.Any(userId => userId != Guid.Empty))
        {
            throw new NonTransientException("Payload does not contain valid RecipientUserIds.");
        }

        var normalizedStatus = message.Status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            throw new NonTransientException("Payload contains unsupported Status.");
        }

        return normalizedStatus;
    }
}
