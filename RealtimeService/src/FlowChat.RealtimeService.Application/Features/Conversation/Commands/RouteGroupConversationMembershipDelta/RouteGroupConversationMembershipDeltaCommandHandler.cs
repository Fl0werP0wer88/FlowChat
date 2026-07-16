using CSharpFunctionalExtensions;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;

public sealed class RouteGroupConversationMembershipDeltaCommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeGroupMembershipRevisionTrackerRepository realtimeGroupMembershipRevisionTrackerRepository,
    IRealtimeEventRouter realtimeEventRouter,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<RouteGroupConversationMembershipDeltaCommand, Unit>(unitOfWork)
{
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeGroupMembershipRevisionTrackerRepository _realtimeGroupMembershipRevisionTrackerRepository = realtimeGroupMembershipRevisionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRevisionTrackerRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        RouteGroupConversationMembershipDeltaCommand request,
        CancellationToken cancellationToken)
    {
        var trackedRevision = await _realtimeGroupMembershipRevisionTrackerRepository.GetRevisionAsync(
            request.ConversationId,
            cancellationToken);

        if (trackedRevision >= request.ProjectionRevision)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var participantUserIds = request.ParticipantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (request.Operation == DeltaOperationType.Added)
        {
            await _realtimeGroupMembershipReadModelRepository.AddRangeAsync(
                participantUserIds,
                RealtimeGroupType.Conversation,
                request.ConversationId,
                cancellationToken);

            await _realtimeGroupMembershipRevisionTrackerRepository.UpsertIfNewerAsync(
                request.ConversationId,
                request.ProjectionRevision,
                cancellationToken);

            await _realtimeEventRouter.RouteGroupConversationParticipantsAddedAsync(
                new GroupConversationParticipantsAddedParam(request.ConversationId, participantUserIds),
                cancellationToken);
        }
        else
        {
            await _realtimeGroupMembershipReadModelRepository.RemoveRangeAsync(
                participantUserIds,
                RealtimeGroupType.Conversation,
                request.ConversationId,
                cancellationToken);

            await _realtimeGroupMembershipRevisionTrackerRepository.UpsertIfNewerAsync(
                request.ConversationId,
                request.ProjectionRevision,
                cancellationToken);

            await _realtimeEventRouter.RouteGroupConversationParticipantsRemovedAsync(
                new GroupConversationParticipantsRemovedParam(request.ConversationId, participantUserIds),
                cancellationToken);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
