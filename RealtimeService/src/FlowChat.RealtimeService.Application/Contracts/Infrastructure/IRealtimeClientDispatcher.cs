namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task ReceiveMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken);

    Task ConversationChangedAsync(ConversationChangedParam notification, CancellationToken cancellationToken);
}
