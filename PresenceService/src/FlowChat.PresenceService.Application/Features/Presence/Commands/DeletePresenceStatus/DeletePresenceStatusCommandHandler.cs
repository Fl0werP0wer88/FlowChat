using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;

public sealed class DeletePresenceStatusCommandHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IPresenceStatusStore presenceStatusStore,
    IIntegrationEventPublisher integrationEventPublisher,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<DeletePresenceStatusCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    private PresenceStatusSnapshot? _previousStatus;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        DeletePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        _previousStatus = null;
        _previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (_previousStatus is null)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var recipients = await contactObserverProjectionReadRepository.GetObserverUserIdsAsync(
            request.UserId,
            cancellationToken);
        var integrationEvent = new PresenceStatusChangedIntegrationEvent
        {
            Key = request.UserId.ToString("D"),
            UserId = request.UserId,
            Status = PresenceStatus.Invisible,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            RecipientUserIds = recipients
                .Where(recipientUserId => recipientUserId != Guid.Empty)
                .Distinct()
                .ToList()
        };

        await presenceStatusStore.DeleteAsync(request.UserId, cancellationToken);
        await integrationEventPublisher.PublishToOutboxAsync(integrationEvent, cancellationToken);
        _previousStatus = null;

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;

    protected override async Task<FlowChatResult<Unit>> HandleUnexpectedExceptionAsync(
        DeletePresenceStatusCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_previousStatus is not null)
            {
                await presenceStatusStore.SetAsync(
                    request.UserId,
                    _previousStatus.Status,
                    _previousStatus.ChangedAtUtc,
                    cancellationToken);
            }
        }
        catch
        {
            // Redis rollback is best-effort because the transactional outbox failure is the primary result for callers
        }
        finally
        {
            _previousStatus = null;
        }

        return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to delete presence status."));
    }
}
