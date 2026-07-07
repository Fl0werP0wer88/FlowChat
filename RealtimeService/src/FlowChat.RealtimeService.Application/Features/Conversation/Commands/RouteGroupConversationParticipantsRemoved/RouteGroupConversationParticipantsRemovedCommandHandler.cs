using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsRemoved;

public sealed class RouteGroupConversationParticipantsRemovedCommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RouteGroupConversationParticipantsRemovedCommand, Unit>
{
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(
        RouteGroupConversationParticipantsRemovedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        var notification = new GroupConversationParticipantsRemovedParam(request.ConversationId, participantUserIds);
        await _realtimeEventRouter.RouteGroupConversationParticipantsRemovedAsync(notification, cancellationToken);


        //Review: to mozna by przesylac w jednym batchu zamiast foreach.
        foreach (var userId in participantUserIds)
        {
            await _realtimeGroupMembershipReadModelRepository.RemoveAsync(
                userId,
                RealtimeGroupType.Conversation,
                request.ConversationId,
                cancellationToken);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
