using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;

public sealed class DeletePresenceStatusCommandHandler(
    IPresenceStatusStore presenceStatusStore,
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher)
    : AggregateRootCommandHandlerBase<DeletePresenceStatusCommand, Unit>(domainEventDispatcher, unitOfWork)
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

        var changedAtUtc = DateTimeOffset.UtcNow;

        await presenceStatusStore.DeleteAsync(request.UserId, cancellationToken);
        await mediator.Publish(
            new PresenceStatusChangedApplicationEvent(
                request.UserId,
                PresenceStatus.Invisible,
                changedAtUtc),
            cancellationToken);
        _previousStatus = null;

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

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
