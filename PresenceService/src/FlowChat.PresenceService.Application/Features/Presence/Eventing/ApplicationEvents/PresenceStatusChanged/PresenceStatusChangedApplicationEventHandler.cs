using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;

public sealed class PresenceStatusChangedApplicationEventHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : INotificationHandler<PresenceStatusChangedApplicationEvent>
{
    public async Task Handle(
        PresenceStatusChangedApplicationEvent notification,
        CancellationToken cancellationToken)
    {
        var recipients = await contactObserverProjectionReadRepository.GetObserverUserIdsAsync(
            notification.UserId,
            cancellationToken);
        var recipientUserIds = recipients
            .Where(recipientUserId => recipientUserId != Guid.Empty)
            .Distinct()
            .ToList();

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var integrationEvent = new PresenceStatusChangedIntegrationEvent
        {
            UserId = notification.UserId,
            Status = notification.Status,
            ChangedAtUtc = notification.ChangedAtUtc,
            RecipientUserIds = recipientUserIds
        };

        await integrationEventPublisher.PublishAsync(
            new IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>(
                integrationEvent,
                notification.UserId.ToString("D")),
            cancellationToken);
    }
}
