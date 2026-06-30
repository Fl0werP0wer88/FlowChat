namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task ReceiveMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken);

    Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken);
}
