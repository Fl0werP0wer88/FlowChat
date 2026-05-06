namespace FlowChat.RealtimeService.Routing;

public interface IUserInstanceRoutingStore
{
    Task RefreshTtlAsync(Guid userId);
}
