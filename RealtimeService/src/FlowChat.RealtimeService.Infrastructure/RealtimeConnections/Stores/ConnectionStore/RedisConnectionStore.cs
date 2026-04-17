using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Redis;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;

internal sealed class RedisConnectionStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeConnectionsSettingsSection settings) : IConnectionStore
{
    private static class HashFields
    {
        public const string UserId = "userId";
        public const string ConnectionId = "connectionId";
        public const string InstanceId = "instanceId";
        public const string ConnectedAtUtc = "connectedAtUtc";
        public const string LastSeenUtc = "lastSeenUtc";
    }

    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public Task UpsertAsync(Guid userId, string connectionId, DateTimeOffset connectedAtUtc, DateTimeOffset lastSeenUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _redisTransactionContext.GetActiveDatabase();
        var connectionKey = GetConnectionKey(connectionId);
        var hashSetTask = database.HashSetAsync(connectionKey,
        [
            new HashEntry(HashFields.UserId, userId.ToString()),
            new HashEntry(HashFields.ConnectionId, connectionId),
            new HashEntry(HashFields.InstanceId, _settings.InstanceId),
            new HashEntry(HashFields.ConnectedAtUtc, connectedAtUtc.ToString("O")),
            new HashEntry(HashFields.LastSeenUtc, lastSeenUtc.ToString("O"))
        ]);
        var expireTask = database.KeyExpireAsync(connectionKey, _settings.ConnectionTtl);

        return CompleteWriteAsync(database, hashSetTask, expireTask);
    }

    public async Task<Guid?> GetUserIdAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var userIdValue = await _redisTransactionContext.GetActiveDatabase()
            .HashGetAsync(GetConnectionKey(connectionId), HashFields.UserId);
        if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out var userId) || userId == Guid.Empty)
        {
            return null;
        }

        return userId;
    }

    public Task<bool> ExistsAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _redisTransactionContext.GetActiveDatabase().KeyExistsAsync(GetConnectionKey(connectionId));
    }

    public Task DeleteAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _redisTransactionContext.GetActiveDatabase();
        var deleteTask = database.KeyDeleteAsync(GetConnectionKey(connectionId));

        return CompleteWriteAsync(database, deleteTask);
    }

    public Task<bool> RefreshTtlAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _redisTransactionContext.GetActiveDatabase()
            .KeyExpireAsync(GetConnectionKey(connectionId), _settings.ConnectionTtl);
    }

    private string GetConnectionKey(string connectionId) => $"{_settings.KeyPrefix}:connections:{connectionId}";

    private static Task CompleteWriteAsync(IDatabaseAsync database, params Task[] operations) =>
        database is ITransaction ? Task.CompletedTask : Task.WhenAll(operations);
}
