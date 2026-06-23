using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;

public sealed class RoutePresenceChangeCommandHandler(IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RoutePresenceChangeCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(RoutePresenceChangeCommand request, CancellationToken cancellationToken)
    {
        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);

        var notification = new PresenceChangedParam(
            request.UserId,
            request.Status,
            request.ChangedAtUtc,
            recipientUserIds);

        await _realtimeEventRouter.RoutePresenceChangeAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeRecipientUserIds(IReadOnlyCollection<Guid> recipientUserIds) =>
        recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
