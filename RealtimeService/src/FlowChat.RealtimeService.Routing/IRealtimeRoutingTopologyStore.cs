namespace FlowChat.RealtimeService.Routing;

public interface IRealtimeRoutingTopologyStore
{
    Task AddConnectionAsync(Guid userId, string instanceId);

    Task RemoveConnectionAsync(Guid userId, string instanceId);

    Task RefreshTtlAsync(Guid userId);
}
