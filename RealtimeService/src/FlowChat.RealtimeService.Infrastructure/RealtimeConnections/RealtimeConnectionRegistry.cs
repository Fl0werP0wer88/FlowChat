using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FlowChat.RealtimeService.Redis.Routing;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RealtimeConnectionRegistry(
    IRealtimeConnectionRedisRepository realtimeConnectionRedisRepository,
    IUserInstanceRoutingRedisRepository userInstanceRoutingRedisRepository,
    IActiveConnectionsTracker activeConnectionsTracker,
    RealtimeConnectionsSettingsSection settings) : IRealtimeConnectionRegistry
{
    private readonly IRealtimeConnectionRedisRepository _realtimeConnectionRedisRepository = realtimeConnectionRedisRepository
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRedisRepository));
    private readonly IUserInstanceRoutingRedisRepository _userInstanceRoutingRedisRepository = userInstanceRoutingRedisRepository
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingRedisRepository));
    private readonly IActiveConnectionsTracker _activeConnectionsTracker = activeConnectionsTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionsTracker));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var nowUtc = DateTimeOffset.UtcNow;
        var result = await _realtimeConnectionRedisRepository.RegisterConnectionAsync(userId, connectionId, nowUtc, cancellationToken);

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
                var storedUserId = await _realtimeConnectionRedisRepository.GetConnectionUserIdAsync(connectionId);
                if (!storedUserId.HasValue || storedUserId.Value == Guid.Empty)
                {
                    await _realtimeConnectionRedisRepository.DeleteConnectionAsync(connectionId);
                    return null;
                }

                userId = storedUserId.Value;
            }

            return await _realtimeConnectionRedisRepository.UnregisterConnectionAsync(
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
            connection => _realtimeConnectionRedisRepository.RefreshConnectionTtlAsync(connection.ConnectionId),
            StringComparer.Ordinal);

        var userSetExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .ToDictionary(
                static connection => connection.UserId,
                connection => _realtimeConnectionRedisRepository.RefreshUserConnectionsTtlAsync(connection.UserId));
        var routingExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .Select(connection => _userInstanceRoutingRedisRepository.RefreshTtlAsync(connection.UserId))
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
