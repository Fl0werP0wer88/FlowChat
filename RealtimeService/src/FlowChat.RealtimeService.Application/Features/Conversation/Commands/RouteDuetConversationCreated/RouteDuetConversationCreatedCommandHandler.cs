using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteDuetConversationCreated;

public sealed class RouteDuetConversationCreatedCommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeGroupMembershipRevisionTrackerRepository realtimeGroupMembershipRevisionTrackerRepository,
    IRealtimeEventRouter realtimeEventRouter,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<RouteDuetConversationCreatedCommand, Unit>(unitOfWork)
{
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeGroupMembershipRevisionTrackerRepository _realtimeGroupMembershipRevisionTrackerRepository = realtimeGroupMembershipRevisionTrackerRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRevisionTrackerRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        RouteDuetConversationCreatedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        await _realtimeGroupMembershipReadModelRepository.AddRangeAsync(
            participantUserIds,
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);

        await _realtimeGroupMembershipRevisionTrackerRepository.UpsertIfNewerAsync(
            request.ConversationId,
            request.ConversationMembershipRevision,
            cancellationToken);

        var notification = new DuetConversationCreatedParam(request.ConversationId, participantUserIds);
        await _realtimeEventRouter.RouteDuetConversationCreatedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
