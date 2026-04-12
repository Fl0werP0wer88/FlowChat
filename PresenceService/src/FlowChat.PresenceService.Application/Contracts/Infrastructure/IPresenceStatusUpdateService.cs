using FlowChat.Core.Messaging.PresenceService.Events;
using MediatR;

namespace FlowChat.PresenceService.Application.Contracts.Infrastructure;

public interface IPresenceStatusUpdateService
{
    Task<FlowChatResult<Unit>> UpdateAndPublishAsync(
        Guid userId,
        Features.Presence.PresenceStatusSnapshot? previousStatus,
        PresenceStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}
