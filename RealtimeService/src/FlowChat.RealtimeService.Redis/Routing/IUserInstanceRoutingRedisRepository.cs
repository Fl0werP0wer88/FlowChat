namespace FlowChat.RealtimeService.Redis.Routing;

public interface IUserInstanceRoutingRedisRepository
{
    Task RefreshTtlAsync(Guid userId);
}
