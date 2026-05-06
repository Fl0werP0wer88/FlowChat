using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores;
using FlowChat.RealtimeService.Routing;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RealtimeConnectionRegistry(
    IRedisRealtimeConnectionStore redisRealtimeConnectionStore,
    IUserInstanceRoutingStore userInstanceRoutingStore,
    IActiveConnectionsTracker activeConnectionsTracker,
    RealtimeConnectionsSettingsSection settings) : IRealtimeConnectionRegistry
{
    private readonly IRedisRealtimeConnectionStore _redisRealtimeConnectionStore = redisRealtimeConnectionStore
        ?? throw new ArgumentNullException(nameof(redisRealtimeConnectionStore));
    private readonly IUserInstanceRoutingStore _userInstanceRoutingStore = userInstanceRoutingStore
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingStore));
    private readonly IActiveConnectionsTracker _activeConnectionsTracker = activeConnectionsTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionsTracker));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var nowUtc = DateTimeOffset.UtcNow;
        var result = await _redisRealtimeConnectionStore.RegisterConnectionAsync(userId, connectionId, nowUtc, cancellationToken);

        _activeConnectionsTracker.Track(userId, connectionId);
        return result;
    }

    public async Task<RealtimeConnectionMutationResult?> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        try
        {
            Guid userId;
            if (_activeConnectionsTracker.TryGet(connectionId, out var trackedConnection) && trackedConnection is not null)
            {
                userId = trackedConnection.UserId;
            }
            else
            {
                var storedUserId = await _redisRealtimeConnectionStore.GetConnectionUserIdAsync(connectionId);
                if (!storedUserId.HasValue || storedUserId.Value == Guid.Empty)
                {
                    await _redisRealtimeConnectionStore.DeleteConnectionAsync(connectionId);
                    return null;
                }

                userId = storedUserId.Value;
            }

            return await _redisRealtimeConnectionStore.UnregisterConnectionAsync(
                userId,
                connectionId,
                _settings.InstanceId,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }
        finally
        {
            _activeConnectionsTracker.Untrack(connectionId);
        }
    }

    public async Task RefreshAsync(IReadOnlyCollection<RealtimeConnectionRefreshEntry> connections, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (connections.Count == 0)
        {
            return;
        }

        var activeConnections = connections
            .Where(static connection => connection.UserId != Guid.Empty && !string.IsNullOrWhiteSpace(connection.ConnectionId))
            .DistinctBy(static connection => connection.ConnectionId)
            .ToArray();

        var connectionExpireTasks = activeConnections.ToDictionary(
            static connection => connection.ConnectionId,
            connection => _redisRealtimeConnectionStore.RefreshConnectionTtlAsync(connection.ConnectionId),
            StringComparer.Ordinal);

        var userSetExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .ToDictionary(
                static connection => connection.UserId,
                connection => _redisRealtimeConnectionStore.RefreshUserConnectionsTtlAsync(connection.UserId));
        var routingExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .Select(connection => _userInstanceRoutingStore.RefreshTtlAsync(connection.UserId))
            .ToArray();

        await Task.WhenAll(connectionExpireTasks.Values.Cast<Task>()
            .Concat(userSetExpireTasks.Values.Cast<Task>())
            .Concat(routingExpireTasks));

        foreach (var (connectionId, expireTask) in connectionExpireTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!expireTask.Result)
            {
                _activeConnectionsTracker.Untrack(connectionId);
            }
        }
    }
}
