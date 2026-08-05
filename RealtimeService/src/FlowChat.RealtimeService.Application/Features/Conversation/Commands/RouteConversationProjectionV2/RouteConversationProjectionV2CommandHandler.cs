using CSharpFunctionalExtensions;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;

public sealed class RouteConversationProjectionV2CommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeGroupMembershipRevisionTrackerRepository realtimeGroupMembershipRevisionTrackerRepository,
    IRealtimeEventRouter realtimeEventRouter,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<RouteConversationProjectionV2Command, Unit>(unitOfWork)
{
    private const int DuetConversationType = 1;

    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository =
        realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeGroupMembershipRevisionTrackerRepository _realtimeGroupMembershipRevisionTrackerRepository =
        realtimeGroupMembershipRevisionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRevisionTrackerRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        RouteConversationProjectionV2Command request,
        CancellationToken cancellationToken)
    {
        if (request.Operation == OperationType.Deleted)
        {
            return UnsupportedOperation(request);
        }

        if (request.Operation == OperationType.Updated && request.ConversationType == DuetConversationType)
        {
            return UnsupportedOperation(request);
        }

        if (request.ConversationType == DuetConversationType)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        //Review4-1: Chyba lepiej żeby Revision membershipu bylo przekazywane razem z command. Worker moze brac je chyba z ProjectionIntegrationEvent<TValue> z TValue (Jeszcze nie ma go tam dodanego ale chat service moglby je przypisywać.) . Wtedy bedziemy mieli pewnosc ze rozsylamy info do aktualnej listy odbiorców.
        //Review4-2: Po zastanowieniu to z tego co widze to ten command nie tworzy zadnej projekcji zgadza sie? W takim wypadku sensowniej uzywac dedykowany  domain integration event MembershipListChanged tylko z id konwersacji (I wtedy Frontend po prostu odswierza sobie liste konwersacji) ,zamiast ProjectionIntegrationEvent Co o tym sądzisz? 
        var trackedRevision = await _realtimeGroupMembershipRevisionTrackerRepository.GetRevisionAsync(
            request.ConversationId,
            cancellationToken);
        if (trackedRevision is null)
        {
            return MembershipNotReady(request.ConversationId);
        }

        var participantUserIds = await _realtimeGroupMembershipReadModelRepository.GetUserIdsByResourceIdAsync(
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);
        var normalizedParticipantUserIds = participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedParticipantUserIds.Length == 0)
        {
            return MembershipNotReady(request.ConversationId);
        }

        if (normalizedParticipantUserIds.Length < 2)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    $"Group conversation {request.ConversationId} has fewer than 2 projected participants."));
        }

        await _realtimeEventRouter.RouteGroupConversationChangedAsync(
            new GroupConversationChangedParam(
                request.ConversationId,
                request.ConversationType,
                request.Name,
                request.CreatedByUserId,
                normalizedParticipantUserIds),
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static FlowChatResult<Unit> MembershipNotReady(Guid conversationId) =>
        FlowChatResult<Unit>.Failure(
            DomainError.UnExpected(
                $"Conversation {conversationId} membership projection is not available yet.",
                FailureKind.Transient));

    private static FlowChatResult<Unit> UnsupportedOperation(RouteConversationProjectionV2Command request) =>
        FlowChatResult<Unit>.Failure(
            DomainError.UnExpected(
                $"Operation {request.Operation} is not supported for conversation type {request.ConversationType}."));
}
