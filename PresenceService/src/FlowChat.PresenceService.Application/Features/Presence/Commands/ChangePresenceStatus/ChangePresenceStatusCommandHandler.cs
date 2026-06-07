using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;

public sealed class ChangePresenceStatusCommandHandler(
    IPresenceStatusStore presenceStatusStore,
    IMediator mediator)
    : CommandHandlerBase<ChangePresenceStatusCommand, Unit>
{
    private PresenceStatusSnapshot? _previousStatus;

    protected override async Task<FlowChatResult<Unit>> HandleCommandAsync(
        ChangePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        _previousStatus = await presenceStatusStore.GetAsync(request.UserId, cancellationToken);
        if (_previousStatus?.Status == request.Status)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var changedAtUtc = DateTimeOffset.UtcNow;

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

        // Active / Busy / Invisible are explicit choices — persist as the default startup status
        if (request.Status is PresenceStatus.Active or PresenceStatus.Busy or PresenceStatus.Invisible)
        {
            await mediator.Send(
                new ChangeUserPresencePreferencesCommand(request.UserId, request.Status),
                cancellationToken);
        }
        // AFK is automatic — leave any saved preference unchanged

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

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
