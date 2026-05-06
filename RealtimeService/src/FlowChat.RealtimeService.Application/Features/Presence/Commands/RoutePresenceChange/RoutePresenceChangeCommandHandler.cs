using CSharpFunctionalExtensions;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;

public sealed class RoutePresenceChangeCommandHandler(IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RoutePresenceChangeCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(RoutePresenceChangeCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("UserId is required."));
        }

        if (!Enum.IsDefined(typeof(PresenceStatus), request.Status))
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Status must be one of: Active, AFK, Busy, Invisible."));
        }

        var recipientUserIds = NormalizeRecipientUserIds(request.RecipientUserIds);
        if (recipientUserIds.Length == 0)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("RecipientUserIds must contain at least one valid user id."));
        }

        var notification = new PresenceChangedNotification(
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
