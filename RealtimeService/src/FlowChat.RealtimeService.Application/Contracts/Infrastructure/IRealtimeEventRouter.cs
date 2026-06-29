namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeEventRouter
{
    Task RouteMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken);

    Task RoutePresenceChangeAsync(PresenceChangedParam notification, CancellationToken cancellationToken);

    Task RouteGroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken);
}
