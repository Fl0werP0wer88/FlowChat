namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;

internal interface IUserConnectionsStore
{
    Task AddConnectionAsync(Guid userId, string connectionId);

    Task<bool> ContainsConnectionAsync(Guid userId, string connectionId);

    Task RemoveConnectionAsync(Guid userId, string connectionId);

    Task<int> GetConnectionCountAsync(Guid userId);

    Task DeleteIfEmptyAsync(Guid userId);

    Task<bool> RefreshTtlAsync(Guid userId);
}
