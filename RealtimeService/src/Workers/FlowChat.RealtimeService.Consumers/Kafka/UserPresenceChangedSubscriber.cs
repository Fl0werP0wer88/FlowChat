using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class UserPresenceChangedSubscriber(
    IMediator mediator,
    ILogger<UserPresenceChangedSubscriber> logger)
    : SubscriberBase<PresenceStatusChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        PresenceStatusChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        Validate(message);

        var command = new RoutePresenceChangeCommand(
            message.UserId,
            message.Status,
            message.ChangedAtUtc,
            message.RecipientUserIds
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray());

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }

    private static void Validate(PresenceStatusChangedIntegrationEvent message)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new NonTransientException("Payload does not contain valid UserId.");
        }

        if (!Enum.IsDefined(typeof(FlowChat.Core.Domain.PresenceStatus), message.Status))
        {
            throw new NonTransientException("Payload does not contain valid Status.");
        }

        if (message.RecipientUserIds is null || !message.RecipientUserIds.Any(userId => userId != Guid.Empty))
        {
            throw new NonTransientException("Payload does not contain valid RecipientUserIds.");
        }
    }
}
