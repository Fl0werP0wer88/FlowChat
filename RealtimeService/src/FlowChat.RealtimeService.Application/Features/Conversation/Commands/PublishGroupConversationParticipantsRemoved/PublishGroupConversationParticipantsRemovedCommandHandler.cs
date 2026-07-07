using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsRemoved;

public sealed class PublishGroupConversationParticipantsRemovedCommandHandler(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IRealtimeGroupManager realtimeGroupManager,
    IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishGroupConversationParticipantsRemovedCommand, Unit>
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IRealtimeGroupManager _realtimeGroupManager = realtimeGroupManager
        ?? throw new ArgumentNullException(nameof(realtimeGroupManager));
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(
        PublishGroupConversationParticipantsRemovedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        await _realtimeClientDispatcher.GroupConversationParticipantsRemovedAsync(
            new GroupConversationParticipantsRemovedParam(request.ConversationId, participantUserIds),
            cancellationToken);

        //Review: Dobrze by bylo pozbyc sie tych foreach'ow i pobierac connectionId w jednym zapytaniu.
        foreach (var userId in participantUserIds)
        {
            var connectionIds = await _realtimeConnectionRegistry.GetConnectionIdsByUserIdAsync(userId, cancellationToken);
            foreach (var connectionId in connectionIds)
            {
                await _realtimeGroupManager.RemoveFromConversationGroupAsync(connectionId, request.ConversationId, cancellationToken);
            }
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
