using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.RefreshPresenceStatus;

public sealed class RefreshPresenceStatusCommandHandler(IPresenceStatusStore presenceStatusStore)
    : CommandHandlerBase<RefreshPresenceStatusCommand, Unit>
{
    protected override async Task<FlowChatResult<Unit>> HandleCommandAsync(
        RefreshPresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        var refreshTasks = request.UserIds
            .Select(userId => presenceStatusStore.RefreshTtlAsync(userId, cancellationToken));

        await Task.WhenAll(refreshTasks);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
