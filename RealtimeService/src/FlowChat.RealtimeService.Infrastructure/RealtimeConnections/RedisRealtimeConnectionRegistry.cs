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
        public const string ConnectedAtUtc = "connectedAtUtc";
        public const string LastSeenUtc = "lastSeenUtc";
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly RealtimeConnectionsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IActiveRealtimeConnectionTracker _activeConnectionTracker = activeConnectionTracker
        ?? throw new ArgumentNullException(nameof(activeConnectionTracker));

    public async Task RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
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
        _activeConnectionTracker.Track(connectionId);
    }

    public async Task UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var connectionKey = GetConnectionKey(connectionId);
            var userIdValue = await database.HashGetAsync(connectionKey, HashFields.UserId);

            if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out var userId) || userId == Guid.Empty)
            {
                await database.KeyDeleteAsync(connectionKey);
                return;
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

    public async Task RefreshAsync(IReadOnlyCollection<string> connectionIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (connectionIds.Count == 0)
        {
            return;
        }

        var database = _connectionMultiplexer.GetDatabase();
        foreach (var connectionId in connectionIds.Where(static connectionId => !string.IsNullOrWhiteSpace(connectionId)).Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connectionKey = GetConnectionKey(connectionId);
            var userIdValue = await database.HashGetAsync(connectionKey, HashFields.UserId);
            if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out var userId) || userId == Guid.Empty)
            {
                _activeConnectionTracker.Untrack(connectionId);
                continue;
            }

            var userConnectionsKey = GetUserConnectionsKey(userId);
            await database.KeyExpireAsync(connectionKey, _settings.ConnectionTtl);
            await database.KeyExpireAsync(userConnectionsKey, _settings.ConnectionTtl);
        }
    }

    private string GetConnectionKey(string connectionId) => $"{_settings.KeyPrefix}:connections:{connectionId}";

    private string GetUserConnectionsKey(Guid userId) => $"{_settings.KeyPrefix}:user-connections:{userId:D}";
}
