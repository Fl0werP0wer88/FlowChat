using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;

public sealed class ChangePresenceStatusCommandHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IPresenceStatusStore presenceStatusStore,
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IUserPresencePreferencesRepository userPresencePreferencesRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<ChangePresenceStatusCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    private PresenceStatusSnapshot? _previousStatus;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        ChangePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        _previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (_previousStatus?.Status == request.Status)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var recipients = await contactObserverProjectionReadRepository.GetObserverUserIdsAsync(
            request.UserId,
            cancellationToken);
        var changedAtUtc = DateTimeOffset.UtcNow;
        var integrationEvent = new PresenceStatusChangedIntegrationEvent
        {
            Key = request.UserId.ToString("D"),
            UserId = request.UserId,
            Status = request.Status,
            ChangedAtUtc = changedAtUtc,
            RecipientUserIds = recipients
                .Where(recipientUserId => recipientUserId != Guid.Empty)
                .Distinct()
                .ToList()
        };

        // Busy / Invisible are manual choices — persist so they survive reconnect
        if (request.Status is PresenceStatus.Busy or PresenceStatus.Invisible)
        {
            await userPresencePreferencesRepository.UpsertAsync(
                request.UserId, request.Status, changedAtUtc, cancellationToken);
        }
        else if (request.Status == PresenceStatus.Active)
        {
            // User explicitly came back online — clear any saved override
            await userPresencePreferencesRepository.DeleteAsync(request.UserId, cancellationToken);
        }
        // AFK is automatic — leave any saved preference unchanged

        await presenceStatusStore.SetAsync(
            request.UserId,
            integrationEvent.Status,
            integrationEvent.ChangedAtUtc,
            cancellationToken);
        await integrationEventPublisher.Publish(integrationEvent, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;

    protected override async Task<FlowChatResult<Unit>> HandleUnexpectedExceptionAsync(
        ChangePresenceStatusCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await RestorePreviousStatusAsync(request.UserId, _previousStatus, cancellationToken);
        }
        finally
        {
            _previousStatus = null;
        }

        return FlowChatResult<Unit>.Failure(
            DomainError.UnExpected("Failed to update presence status."));
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
