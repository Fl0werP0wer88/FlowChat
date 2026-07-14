using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteDuetConversationCreated;

public sealed class RouteDuetConversationCreatedCommandHandler(
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeEventRouter realtimeEventRouter)
    : ICommandHandler<RouteDuetConversationCreatedCommand, Unit>
{
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));

    public async Task<FlowChatResult<Unit>> Handle(
        RouteDuetConversationCreatedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        await _realtimeGroupMembershipReadModelRepository.AddRangeAsync(
            participantUserIds,
            RealtimeGroupType.Conversation,
            request.ConversationId,
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
