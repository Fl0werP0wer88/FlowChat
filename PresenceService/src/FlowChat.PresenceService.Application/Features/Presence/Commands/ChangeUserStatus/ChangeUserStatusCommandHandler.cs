using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserStatus;

public sealed class ChangeUserStatusCommandHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IPresenceStatusStore presenceStatusStore,
    IPresenceStatusUpdateService presenceStatusUpdateService)
    : ICommandHandler<ChangeUserStatusCommand, Unit>
{
    public async Task<FlowChatResult<Unit>> Handle(
        ChangeUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        var previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (previousStatus?.Status == request.Status)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var recipients = await contactObserverProjectionReadRepository.GetObserverUserIdsAsync(
            request.UserId,
            cancellationToken);
        var changedAtUtc = DateTimeOffset.UtcNow;
        var integrationEvent = new UserStatusChangedIntegrationEvent
        {
            Key = request.UserId.ToString("D"),
            UserId = request.UserId,
            Status = request.Status,
            ChangedAtUtc = changedAtUtc,
            RecipientUserIds = recipients
                .Where(recipientUserId => recipientUserId != Guid.Empty)
                .Distinct()
                .ToArray()
        };

        return await presenceStatusUpdateService.UpdateAndPublishAsync(
            request.UserId,
            previousStatus,
            integrationEvent,
            cancellationToken);
    }
}
