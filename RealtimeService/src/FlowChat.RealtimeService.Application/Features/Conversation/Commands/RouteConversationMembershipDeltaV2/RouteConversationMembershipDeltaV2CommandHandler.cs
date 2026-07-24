using CSharpFunctionalExtensions;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;

public sealed class RouteConversationMembershipDeltaV2CommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeGroupMembershipRevisionTrackerRepository realtimeGroupMembershipRevisionTrackerRepository,
    IRealtimeEventRouter realtimeEventRouter,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<RouteConversationMembershipDeltaV2Command, Unit>(unitOfWork)
{
    private const int MembershipRevisionBaseline = 1;

    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository =
        realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeGroupMembershipRevisionTrackerRepository _realtimeGroupMembershipRevisionTrackerRepository =
        realtimeGroupMembershipRevisionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRevisionTrackerRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        RouteConversationMembershipDeltaV2Command request,
        CancellationToken cancellationToken)
    {
        var trackedRevision = await _realtimeGroupMembershipRevisionTrackerRepository.GetRevisionAsync(
            request.ConversationId,
            cancellationToken);
        var currentRevision = trackedRevision ?? MembershipRevisionBaseline;

        if (request.ProjectionRevision <= currentRevision)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        if (request.ProjectionRevision != currentRevision + 1)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Conversation {request.ConversationId} membership projection revision {currentRevision} cannot be advanced to {request.ProjectionRevision} because an earlier delta is missing.",
                    FailureKind.Transient));
        }

        var addedParticipantUserIds = request.Delta
            .Where(item => item.Operation == OperationType.Created)
            .Select(item => item.ParticipantUserId)
            .ToArray();
        var removedParticipantUserIds = request.Delta
            .Where(item => item.Operation == OperationType.Deleted)
            .Select(item => item.ParticipantUserId)
            .ToArray();

        await _realtimeGroupMembershipReadModelRepository.AddRangeAsync(
            addedParticipantUserIds,
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);

        await _realtimeGroupMembershipReadModelRepository.RemoveRangeAsync(
            removedParticipantUserIds,
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);

        await _realtimeGroupMembershipRevisionTrackerRepository.UpsertIfNewerAsync(
            request.ConversationId,
            request.ProjectionRevision,
            cancellationToken);

        if (trackedRevision is null)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }



        //Review4-2: Ztego co rozumiem to dwa kolejne wywoania RouteGroupConversationParticipantsAddedAsync  będą tylko notyfikowały frontend o zmiane listy zgadza sie? W takim wypadku sensowniej uzywac dedykowany  domain integration event MembershipListChanged tylko z id membership (I wtedy Frontend po prostu odswierza sobie liste uzytkowników) ,zamiast ProjectionIntegrationEvent Co o tym sądzisz? 
        if (addedParticipantUserIds.Length > 0)
        {
            await _realtimeEventRouter.RouteGroupConversationParticipantsAddedAsync(
                new GroupConversationParticipantsAddedParam(request.ConversationId, addedParticipantUserIds),
                cancellationToken);
        }

        if (removedParticipantUserIds.Length > 0)
        {
            await _realtimeEventRouter.RouteGroupConversationParticipantsRemovedAsync(
                new GroupConversationParticipantsRemovedParam(request.ConversationId, removedParticipantUserIds),
                cancellationToken);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
