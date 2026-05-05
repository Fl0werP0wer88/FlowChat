using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.RealTimeStore;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;
using FlowChat.RealtimeService.Routing;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RealtimeConnectionRegistry(
    IConnectionStore connectionStore,
    IUserConnectionsStore userConnectionsStore,
    IUserInstanceRoutingStore userInstanceRoutingStore,
    IActiveConnectionsTracker activeConnectionsTracker,
    IRealTimeStore realTimeStore,
    RealtimeConnectionsSettingsSection settings) : IRealtimeConnectionRegistry
{
    private readonly IConnectionStore _connectionStore = connectionStore
        ?? throw new ArgumentNullException(nameof(connectionStore));
    private readonly IUserConnectionsStore _userConnectionsStore = userConnectionsStore
        ?? throw new ArgumentNullException(nameof(userConnectionsStore));
    private readonly IUserInstanceRoutingStore _userInstanceRoutingStore = userInstanceRoutingStore
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingStore));
    private readonly IActiveConnectionsTracker _activeConnectionsTracker = activeConnectionsTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionsTracker));
    private readonly IRealTimeStore _realTimeStore = realTimeStore ?? throw new ArgumentNullException(nameof(realTimeStore));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var nowUtc = DateTimeOffset.UtcNow;
        var result = await _realTimeStore.RegisterConnectionAsync(userId, connectionId, nowUtc, cancellationToken);

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
                var storedUserId = await _connectionStore.GetUserIdAsync(connectionId);
                if (!storedUserId.HasValue || storedUserId.Value == Guid.Empty)
                {
                    await _connectionStore.DeleteAsync(connectionId);
                    return null;
                }

                userId = storedUserId.Value;
            }

            var connectionExists = await _connectionStore.ExistsAsync(connectionId);
            var connectionInUserSet = await _userConnectionsStore.ContainsConnectionAsync(userId, connectionId);
            if (!connectionExists && !connectionInUserSet)
            {
                return null;
            }

            await _userConnectionsStore.RemoveConnectionAsync(userId, connectionId);
            await _connectionStore.DeleteAsync(connectionId);
            await _userInstanceRoutingStore.RemoveConnectionAsync(userId, _settings.InstanceId);
            var activeConnectionCount = await _userConnectionsStore.GetConnectionCountAsync(userId);

            if (activeConnectionCount == 0)
            {
                await _userConnectionsStore.DeleteIfEmptyAsync(userId);
            }

            return new RealtimeConnectionMutationResult(
                userId,
                connectionId,
                activeConnectionCount,
                false,
                DateTimeOffset.UtcNow);
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
            connection => _connectionStore.RefreshTtlAsync(connection.ConnectionId),
            StringComparer.Ordinal);

        var userSetExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .ToDictionary(
                static connection => connection.UserId,
                connection => _userConnectionsStore.RefreshTtlAsync(connection.UserId));
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
