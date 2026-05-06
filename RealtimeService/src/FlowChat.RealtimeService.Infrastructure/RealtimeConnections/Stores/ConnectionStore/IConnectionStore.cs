namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;

internal interface IConnectionStore
{
    Task<Guid?> GetUserIdAsync(string connectionId);

    Task DeleteAsync(string connectionId);

    Task<bool> RefreshTtlAsync(string connectionId);
}
