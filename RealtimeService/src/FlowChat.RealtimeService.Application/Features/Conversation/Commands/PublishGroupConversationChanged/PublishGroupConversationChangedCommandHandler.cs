using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationChanged;

public sealed class PublishGroupConversationChangedCommandHandler(IRealtimeClientDispatcher realtimeClientDispatcher)
    : ICommandHandler<PublishGroupConversationChangedCommand, Unit>
{
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));

    public async Task<FlowChatResult<Unit>> Handle(
        PublishGroupConversationChangedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = NormalizeParticipantUserIds(request.ParticipantUserIds);

        var notification = new GroupConversationChangedParam(
            request.ConversationId,
            request.Type,
            request.Name,
            request.CreatedByUserId,
            participantUserIds);

        await _realtimeClientDispatcher.GroupConversationChangedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    private static Guid[] NormalizeParticipantUserIds(IReadOnlyCollection<Guid> participantUserIds) =>
        participantUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
}
