namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeEventRouter
{
    Task RouteMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task RoutePresenceChangeAsync(PresenceChangedParam notification, CancellationToken cancellationToken);
}
