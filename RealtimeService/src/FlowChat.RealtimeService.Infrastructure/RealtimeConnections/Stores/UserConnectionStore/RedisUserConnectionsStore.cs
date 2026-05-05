using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Routing;
using FlowChat.Shared.Infrastructure.Redis;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;

internal sealed class RedisUserConnectionsStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeConnectionsSettingsSection settings) : IUserConnectionsStore
{
    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public Task AddConnectionAsync(Guid userId, string connectionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _redisTransactionContext.GetActiveDatabase();
        var userConnectionsKey = RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId);
        var addTask = database.SetAddAsync(userConnectionsKey, connectionId);
        var expireTask = database.KeyExpireAsync(userConnectionsKey, _settings.ConnectionTtl);

        return CompleteWriteAsync(database, addTask, expireTask);
    }

    public Task<bool> ContainsConnectionAsync(Guid userId, string connectionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _redisTransactionContext.GetActiveDatabase()
            .SetContainsAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId), connectionId);
    }

    public Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _redisTransactionContext.GetActiveDatabase();
        var removeTask = database.SetRemoveAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId), connectionId);

        return CompleteWriteAsync(database, removeTask);
    }

    public async Task<int> GetConnectionCountAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return checked((int)await _redisTransactionContext.GetActiveDatabase()
            .SetLengthAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId)));
    }

    public async Task DeleteIfEmptyAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        if (await GetConnectionCountAsync(userId) != 0)
        {
            return;
        }

        var database = _redisTransactionContext.GetActiveDatabase();
        var deleteTask = database.KeyDeleteAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId));

        await CompleteWriteAsync(database, deleteTask);
    }

    public Task<bool> RefreshTtlAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return _redisTransactionContext.GetActiveDatabase()
            .KeyExpireAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId), _settings.ConnectionTtl);
    }

    private static Task CompleteWriteAsync(IDatabaseAsync database, params Task[] operations) =>
        database is ITransaction ? Task.CompletedTask : Task.WhenAll(operations);
}
