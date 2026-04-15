using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RedisRealtimeConnectionRegistry(
    IConnectionStore connectionStore,
    IUserConnectionsStore userConnectionsStore,
    IUnitOfWork unitOfWork,
    IActiveRealtimeConnectionTracker activeConnectionTracker) : IRealtimeConnectionRegistry
{
    private readonly IConnectionStore _connectionStore = connectionStore
        ?? throw new ArgumentNullException(nameof(connectionStore));
    private readonly IUserConnectionsStore _userConnectionsStore = userConnectionsStore
        ?? throw new ArgumentNullException(nameof(userConnectionsStore));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IActiveRealtimeConnectionTracker _activeConnectionTracker = activeConnectionTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionTracker));

    public async Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var nowUtc = DateTimeOffset.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            await _connectionStore.UpsertAsync(userId, connectionId, nowUtc, nowUtc);
            await _userConnectionsStore.AddConnectionAsync(userId, connectionId);

            return 0;
        }, cancellationToken);

        _activeConnectionTracker.Track(userId, connectionId);
        var activeConnectionCount = await _userConnectionsStore.GetConnectionCountAsync(userId);

        return new RealtimeConnectionMutationResult(
            userId,
            connectionId,
            activeConnectionCount,
            nowUtc);
    }

    public async Task<RealtimeConnectionMutationResult?> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        try
        {
            Guid userId;
            if (_activeConnectionTracker.TryGet(connectionId, out var trackedConnection) && trackedConnection is not null)
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
            var activeConnectionCount = await _userConnectionsStore.GetConnectionCountAsync(userId);

            if (activeConnectionCount == 0)
            {
                await _userConnectionsStore.DeleteIfEmptyAsync(userId);
            }

            return new RealtimeConnectionMutationResult(
                userId,
                connectionId,
                activeConnectionCount,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            _activeConnectionTracker.Untrack(connectionId);
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

        await Task.WhenAll(connectionExpireTasks.Values.Cast<Task>().Concat(userSetExpireTasks.Values.Cast<Task>()));

        foreach (var (connectionId, expireTask) in connectionExpireTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!expireTask.Result)
            {
                _activeConnectionTracker.Untrack(connectionId);
            }
        }
    }
}
