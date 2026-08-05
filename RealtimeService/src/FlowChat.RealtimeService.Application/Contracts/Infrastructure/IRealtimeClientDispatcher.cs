namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task MessageReceivedAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken);

    Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken);

    Task ConversationParticipantsAddedAsync(ConversationParticipantsAddedParam notification, CancellationToken cancellationToken);

    Task ConversationParticipantsRemovedAsync(ConversationParticipantsRemovedParam notification, CancellationToken cancellationToken);
}
