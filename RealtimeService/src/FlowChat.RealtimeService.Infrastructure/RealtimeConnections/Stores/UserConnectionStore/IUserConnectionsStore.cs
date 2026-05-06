namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;

internal interface IUserConnectionsStore
{
    Task<bool> RefreshTtlAsync(Guid userId);
}
