using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;

public sealed class InitializePresenceStatusCommandHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IPresenceStatusStore presenceStatusStore,
    IOutboxIntegrationEventPublisher integrationEventPublisher,
    IUserPresencePreferencesReadRepository userPresencePreferencesReadRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<InitializePresenceStatusCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    private PresenceStatusSnapshot? _previousStatus;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        InitializePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        _previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (_previousStatus is not null)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var recipients = await contactObserverProjectionReadRepository.GetObserverUserIdsAsync(
            request.UserId,
            cancellationToken);
        var changedAtUtc = DateTimeOffset.UtcNow;

        // Restore any saved manual preference (Busy/Invisible); default to Active otherwise
        var preference = await userPresencePreferencesReadRepository.FindPreferredStatusAsync(request.UserId, cancellationToken);
        var statusToSet = preference ?? PresenceStatus.Active;

        var integrationEvent = new PresenceStatusChangedIntegrationEvent
        {
            Key = request.UserId.ToString("D"),
            UserId = request.UserId,
            Status = statusToSet,
            ChangedAtUtc = changedAtUtc,
            RecipientUserIds = recipients
                .Where(recipientUserId => recipientUserId != Guid.Empty)
                .Distinct()
                .ToList()
        };

        await presenceStatusStore.SetAsync(
            request.UserId,
            integrationEvent.Status,
            integrationEvent.ChangedAtUtc,
            cancellationToken);
        await integrationEventPublisher.Publish(integrationEvent, cancellationToken);
        _previousStatus = null;

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;

    protected override async Task<FlowChatResult<Unit>> HandleUnexpectedExceptionAsync(
        InitializePresenceStatusCommand request,
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
            DomainError.UnExpected("Failed to initialize presence status."));
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
