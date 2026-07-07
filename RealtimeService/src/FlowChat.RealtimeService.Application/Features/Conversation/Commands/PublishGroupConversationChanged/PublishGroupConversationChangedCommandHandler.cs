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
        var notification = new GroupConversationChangedParam(
            request.ConversationId,
            request.Type,
            request.Name,
            request.CreatedByUserId,
            []);

        await _realtimeClientDispatcher.GroupConversationChangedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
