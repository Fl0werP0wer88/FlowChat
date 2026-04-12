using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class RedisRealtimeConnectionRegistry(
    IConnectionMultiplexer connectionMultiplexer,
    RealtimeConnectionsSettings settings,
    IActiveRealtimeConnectionTracker activeConnectionTracker) : IRealtimeConnectionRegistry
{
    private static class HashFields
    {
        public const string UserId = "userId";
        public const string ConnectionId = "connectionId";
        public const string InstanceId = "instanceId";
        public const string Status = "status";
        public const string ConnectedAtUtc = "connectedAtUtc";
        public const string LastSeenUtc = "lastSeenUtc";
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly RealtimeConnectionsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IActiveRealtimeConnectionTracker _activeConnectionTracker = activeConnectionTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionTracker));

    public async Task RegisterAsync(Guid userId, string connectionId, UserPresenceStatus status, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _connectionMultiplexer.GetDatabase();
        var nowUtc = DateTimeOffset.UtcNow;
        var connectionKey = GetConnectionKey(connectionId);
        var userConnectionsKey = GetUserConnectionsKey(userId);

        var transaction = database.CreateTransaction();
        var hashSetTask = transaction.HashSetAsync(connectionKey,
        [
            new HashEntry(HashFields.UserId, userId.ToString()),
            new HashEntry(HashFields.ConnectionId, connectionId),
            new HashEntry(HashFields.InstanceId, _settings.InstanceId),
            new HashEntry(HashFields.Status, status.ToString()),
            new HashEntry(HashFields.ConnectedAtUtc, nowUtc.ToString("O")),
            new HashEntry(HashFields.LastSeenUtc, nowUtc.ToString("O"))
        ]);
        var addToSetTask = transaction.SetAddAsync(userConnectionsKey, connectionId);
        var expireConnectionTask = transaction.KeyExpireAsync(connectionKey, _settings.ConnectionTtl);
        var expireUserSetTask = transaction.KeyExpireAsync(userConnectionsKey, _settings.ConnectionTtl);

        var committed = await transaction.ExecuteAsync();
        if (!committed)
        {
            throw new InvalidOperationException($"Failed to register realtime connection '{connectionId}' in Redis.");
        }

        await Task.WhenAll(hashSetTask, addToSetTask, expireConnectionTask, expireUserSetTask);
        _activeConnectionTracker.Track(userId, connectionId, status);
    }

    public async Task UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var connectionKey = GetConnectionKey(connectionId);

            Guid userId;
            if (_activeConnectionTracker.TryGet(connectionId, out var trackedConnection) && trackedConnection is not null)
            {
                userId = trackedConnection.UserId;
            }
            else
            {
                var userIdValue = await database.HashGetAsync(connectionKey, HashFields.UserId);
                if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out userId) || userId == Guid.Empty)
                {
                    await database.KeyDeleteAsync(connectionKey);
                    return;
                }
            }

            var userConnectionsKey = GetUserConnectionsKey(userId);
            await database.SetRemoveAsync(userConnectionsKey, connectionId);
            await database.KeyDeleteAsync(connectionKey);

            if (await database.SetLengthAsync(userConnectionsKey) == 0)
            {
                await database.KeyDeleteAsync(userConnectionsKey);
            }
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

        var database = _connectionMultiplexer.GetDatabase();
        var activeConnections = connections
            .Where(static connection => connection.UserId != Guid.Empty && !string.IsNullOrWhiteSpace(connection.ConnectionId))
            .DistinctBy(static connection => connection.ConnectionId)
            .ToArray();

        var connectionExpireTasks = activeConnections.ToDictionary(
            static connection => connection.ConnectionId,
            connection => database.KeyExpireAsync(GetConnectionKey(connection.ConnectionId), _settings.ConnectionTtl),
            StringComparer.Ordinal);

        var userSetExpireTasks = activeConnections
            .DistinctBy(static connection => connection.UserId)
            .ToDictionary(
                static connection => connection.UserId,
                connection => database.KeyExpireAsync(GetUserConnectionsKey(connection.UserId), _settings.ConnectionTtl));

        await Task.WhenAll(connectionExpireTasks.Values.Concat(userSetExpireTasks.Values));

        foreach (var (connectionId, expireTask) in connectionExpireTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!expireTask.Result)
            {
                _activeConnectionTracker.Untrack(connectionId);
            }
        }
    }

    private string GetConnectionKey(string connectionId) => $"{_settings.KeyPrefix}:connections:{connectionId}";

    private string GetUserConnectionsKey(Guid userId) => $"{_settings.KeyPrefix}:user-connections:{userId:D}";
}
