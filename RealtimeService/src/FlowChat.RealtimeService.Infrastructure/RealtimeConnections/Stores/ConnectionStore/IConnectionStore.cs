namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;

internal interface IConnectionStore
{
    Task UpsertAsync(Guid userId, string connectionId, DateTimeOffset connectedAtUtc, DateTimeOffset lastSeenUtc);

    Task<Guid?> GetUserIdAsync(string connectionId);

    Task<bool> ExistsAsync(string connectionId);

    Task DeleteAsync(string connectionId);

    Task<bool> RefreshTtlAsync(string connectionId);
}
