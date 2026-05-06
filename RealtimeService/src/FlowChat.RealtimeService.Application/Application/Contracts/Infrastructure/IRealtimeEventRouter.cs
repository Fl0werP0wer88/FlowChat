namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeEventRouter
{
    Task RouteMessageAsync(ChatMessageNotification notification, CancellationToken cancellationToken);

    Task RoutePresenceChangeAsync(PresenceChangedNotification notification, CancellationToken cancellationToken);
}
