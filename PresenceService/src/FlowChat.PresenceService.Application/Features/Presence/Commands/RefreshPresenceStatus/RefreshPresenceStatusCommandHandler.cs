using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.RefreshPresenceStatus;

public sealed class RefreshPresenceStatusCommandHandler(IPresenceStatusStore presenceStatusStore)
    : IRequestHandler<RefreshPresenceStatusCommand, FlowChatResult<Unit>>
{
    private readonly IPresenceStatusStore _presenceStatusStore = presenceStatusStore
        ?? throw new ArgumentNullException(nameof(presenceStatusStore));

    public async Task<FlowChatResult<Unit>> Handle(
        RefreshPresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        var refreshTasks = request.UserIds
            .Select(userId => _presenceStatusStore.RefreshTtlAsync(userId, cancellationToken));

        await Task.WhenAll(refreshTasks);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
