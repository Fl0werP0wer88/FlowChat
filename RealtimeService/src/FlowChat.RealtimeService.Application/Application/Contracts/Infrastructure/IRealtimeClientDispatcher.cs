namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeClientDispatcher
{
    Task ReceiveMessageAsync(ChatMessageNotification notification, CancellationToken cancellationToken);

    Task PresenceChangedAsync(PresenceChangedNotification notification, CancellationToken cancellationToken);
}
