using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;

public sealed class ChangePresenceStatusCommandHandler(
    IPresenceStatusStore presenceStatusStore,
    IUserPresencePreferencesWriteRepository userPresencePreferencesWriteRepository,
    IMediator mediator,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<ChangePresenceStatusCommand, Unit>(unitOfWork)
{
    private PresenceStatusSnapshot? _previousStatus;

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        ChangePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        _previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (_previousStatus?.Status == request.Status)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var changedAtUtc = DateTimeOffset.UtcNow;

        // Busy / Invisible are manual choices — persist so they survive reconnect
        if (request.Status is PresenceStatus.Busy or PresenceStatus.Invisible)
        {
            var preferences = await userPresencePreferencesWriteRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);
            if (preferences is null)
            {
                await userPresencePreferencesWriteRepository.AddAsync(
                    UserPresencePreferences.Create(request.UserId, request.Status),
                    cancellationToken);
            }
            else
            {
                preferences.SetPreferredStatus(request.Status);
            }
        }
        else if (request.Status == PresenceStatus.Active)
        {
            // User explicitly came back online — clear any saved override
            var preferences = await userPresencePreferencesWriteRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);
            if (preferences is not null)
            {
                await userPresencePreferencesWriteRepository.DeleteAsync(preferences, cancellationToken);
            }
        }
        // AFK is automatic — leave any saved preference unchanged

        await presenceStatusStore.SetAsync(
            request.UserId,
            request.Status,
            changedAtUtc,
            cancellationToken);

        await mediator.Publish(
            new PresenceStatusChangedApplicationEvent(
                request.UserId,
                request.Status,
                changedAtUtc),
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    //ToDo: we will be providin idempotendy in deifferent way than on command handlers. Remmeber to remove.
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
