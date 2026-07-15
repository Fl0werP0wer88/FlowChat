namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task MessageReceivedAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken);

    Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken);

    Task GroupConversationParticipantsAddedAsync(GroupConversationParticipantsAddedParam notification, CancellationToken cancellationToken);

    Task GroupConversationParticipantsRemovedAsync(GroupConversationParticipantsRemovedParam notification, CancellationToken cancellationToken);

    Task DuetConversationsListChangedAsync(Guid conversationId, CancellationToken cancellationToken);
}
