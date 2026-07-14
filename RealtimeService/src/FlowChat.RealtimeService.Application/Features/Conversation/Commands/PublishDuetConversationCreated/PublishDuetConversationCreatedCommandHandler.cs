using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishDuetConversationCreated;

public sealed class PublishDuetConversationCreatedCommandHandler(
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IRealtimeGroupManager realtimeGroupManager)
    : ICommandHandler<PublishDuetConversationCreatedCommand, Unit>
{
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IRealtimeGroupManager _realtimeGroupManager = realtimeGroupManager
        ?? throw new ArgumentNullException(nameof(realtimeGroupManager));

    public async Task<FlowChatResult<Unit>> Handle(
        PublishDuetConversationCreatedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        var connectionIdsByUser = await _realtimeConnectionRegistry.GetConnectionIdsByUserIdsAsync(participantUserIds, cancellationToken);
        foreach (var connectionId in connectionIdsByUser.Values.SelectMany(static connectionIds => connectionIds))
        {
            await _realtimeGroupManager.AddToConversationGroupAsync(connectionId, request.ConversationId, cancellationToken);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
