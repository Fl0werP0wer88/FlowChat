using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Infrastructure.Presence;

internal sealed class PresenceStatusUpdateService(
    IPresenceStatusStore presenceStatusStore,
    IIntegrationEventPublisher integrationEventPublisher,
    IUnitOfWork unitOfWork) : IPresenceStatusUpdateService
{
    public async Task<FlowChatResult<Unit>> UpdateAndPublishAsync(
        Guid userId,
        PresenceStatusSnapshot? previousStatus,
        UserStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await presenceStatusStore.SetAsync(
                    userId,
                    integrationEvent.Status,
                    integrationEvent.ChangedAtUtc,
                    token);
                await integrationEventPublisher.PublishToOutboxAsync(integrationEvent, token);

                return FlowChatResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
        catch (Exception)
        {
            await RestorePreviousStatusAsync(userId, previousStatus, cancellationToken);

            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected("Failed to update user status."));
        }
    }

    private async Task RestorePreviousStatusAsync(
        Guid userId,
        PresenceStatusSnapshot? previousStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            if (previousStatus is null)
            {
                await presenceStatusStore.DeleteAsync(userId, cancellationToken);
                return;
            }

            await presenceStatusStore.SetAsync(
                userId,
                previousStatus.Status,
                previousStatus.ChangedAtUtc,
                cancellationToken);
        }
        catch
        {
            // Redis rollback is best-effort because the transactional outbox failure is the primary result for callers
        }
    }
}
