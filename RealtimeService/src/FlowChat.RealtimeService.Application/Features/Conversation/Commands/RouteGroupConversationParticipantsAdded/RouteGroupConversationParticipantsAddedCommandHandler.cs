using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsAdded;
//ToDo1: To powinno dziedziczyz TransactionalCommandHandlerBase aby uruchamialo sie w transakcji.
public sealed class RouteGroupConversationParticipantsAddedCommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeGroupMembershipVersionTrackerRepository realtimeGroupMembershipVersionTrackerRepository,
    IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RouteGroupConversationParticipantsAddedCommand, Unit>
{
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeGroupMembershipVersionTrackerRepository _realtimeGroupMembershipVersionTrackerRepository = realtimeGroupMembershipVersionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipVersionTrackerRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(
        RouteGroupConversationParticipantsAddedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        await _realtimeGroupMembershipReadModelRepository.AddRangeAsync(
            participantUserIds,
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);

        await _realtimeGroupMembershipVersionTrackerRepository.UpsertIfNewerAsync(
            request.ConversationId,
            request.ConversationVersion,
            cancellationToken);

        var notification = new GroupConversationParticipantsAddedParam(request.ConversationId, participantUserIds);
        await _realtimeEventRouter.RouteGroupConversationParticipantsAddedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
