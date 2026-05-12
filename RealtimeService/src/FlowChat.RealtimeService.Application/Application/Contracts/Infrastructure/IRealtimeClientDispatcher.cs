namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task ReceiveMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken);
}
