namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserInstanceRoutingStore;

internal interface IUserInstanceRoutingStore
{
    Task AddConnectionAsync(Guid userId, string instanceId);

    Task RemoveConnectionAsync(Guid userId, string instanceId);

    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);

    Task RefreshTtlAsync(Guid userId);
}
