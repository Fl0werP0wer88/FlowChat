using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsAdded;

public sealed class PublishGroupConversationParticipantsAddedCommandHandler(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IRealtimeGroupManager realtimeGroupManager,
    IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishGroupConversationParticipantsAddedCommand, Unit>
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IRealtimeGroupManager _realtimeGroupManager = realtimeGroupManager
        ?? throw new ArgumentNullException(nameof(realtimeGroupManager));
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(
        PublishGroupConversationParticipantsAddedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        var connectionIdsByUser = await _realtimeConnectionRegistry.GetConnectionIdsByUserIdsAsync(participantUserIds, cancellationToken);
        foreach (var connectionId in connectionIdsByUser.Values.SelectMany(static connectionIds => connectionIds))
        {
            await _realtimeGroupManager.AddToConversationGroupAsync(connectionId, request.ConversationId, cancellationToken);
        }

        await _realtimeClientDispatcher.GroupConversationParticipantsAddedAsync(
            new GroupConversationParticipantsAddedParam(request.ConversationId, participantUserIds),
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
