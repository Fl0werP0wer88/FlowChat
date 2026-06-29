using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;

public sealed class RouteGroupConversationChangedCommandHandler(IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RouteGroupConversationChangedCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(
        RouteGroupConversationChangedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        var notification = new GroupConversationChangedParam(
            request.ConversationId,
            request.Type,
            request.Name,
            request.CreatedByUserId,
            participantUserIds);

        await _realtimeEventRouter.RouteGroupConversationChangedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
