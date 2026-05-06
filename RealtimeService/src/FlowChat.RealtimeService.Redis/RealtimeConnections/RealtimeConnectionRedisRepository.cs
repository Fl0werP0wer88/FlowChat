using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.Routing;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Redis.RealtimeConnections;

public sealed class RealtimeConnectionRedisRepository(
    IConnectionMultiplexer connectionMultiplexer,
    RealtimeConnectionsSettingsSection settings) : IRealtimeConnectionRedisRepository
{
    private const string RegisterConnectionScript = """
        redis.call(
            'HSET',
            KEYS[1],
            'userId',
            ARGV[1],
            'connectionId',
            ARGV[2],
            'instanceId',
            ARGV[3],
            'connectedAtUtc',
            ARGV[4],
            'lastSeenUtc',
            ARGV[5])
        redis.call('PEXPIRE', KEYS[1], ARGV[6])

        local added = redis.call('SADD', KEYS[2], ARGV[2])
        redis.call('PEXPIRE', KEYS[2], ARGV[6])

        local activeConnectionCount = redis.call('SCARD', KEYS[2])
        local isFirstConnectionForUser = 0

        if added == 1 then
            redis.call('HINCRBY', KEYS[4], ARGV[3], 1)
            redis.call('SADD', KEYS[3], ARGV[3])

            if activeConnectionCount == 1 then
                isFirstConnectionForUser = 1
            end
        end

        redis.call('PEXPIRE', KEYS[3], ARGV[6])
        redis.call('PEXPIRE', KEYS[4], ARGV[6])

        return { activeConnectionCount, isFirstConnectionForUser }
        """;

    private const string UnregisterConnectionScript = """
        local removed = redis.call('SREM', KEYS[2], ARGV[1])
        redis.call('DEL', KEYS[1])

        local activeConnectionCount = redis.call('SCARD', KEYS[2])
        if activeConnectionCount == 0 then
            redis.call('DEL', KEYS[2])
        end

        local isLastConnectionForUser = 0
        if removed == 1 then
            local instanceConnectionCount = redis.call('HINCRBY', KEYS[4], ARGV[2], -1)
            if instanceConnectionCount <= 0 then
                redis.call('HDEL', KEYS[4], ARGV[2])
                redis.call('SREM', KEYS[3], ARGV[2])
            end

            if redis.call('HLEN', KEYS[4]) == 0 then
                redis.call('DEL', KEYS[4])
            end

            if redis.call('SCARD', KEYS[3]) == 0 then
                redis.call('DEL', KEYS[3])
            end

            if activeConnectionCount == 0 then
                isLastConnectionForUser = 1
            end
        end

        return { activeConnectionCount, isLastConnectionForUser, removed }
        """;

    private static class HashFields
    {
        public const string UserId = "userId";
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<Guid?> GetConnectionUserIdAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var userIdValue = await _connectionMultiplexer.GetDatabase()
            .HashGetAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId), HashFields.UserId);
        if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out var userId) || userId == Guid.Empty)
        {
            return null;
        }

        return userId;
    }

    public Task DeleteConnectionAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _connectionMultiplexer.GetDatabase()
            .KeyDeleteAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId));
    }

    public Task<bool> RefreshConnectionTtlAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _connectionMultiplexer.GetDatabase()
            .KeyExpireAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId), _settings.ConnectionTtl);
    }

    public Task<bool> RefreshUserConnectionsTtlAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return _connectionMultiplexer.GetDatabase()
            .KeyExpireAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId), _settings.ConnectionTtl);
    }

    public async Task<RealtimeConnectionMutationResult> RegisterConnectionAsync(
        Guid userId,
        string connectionId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var database = _connectionMultiplexer.GetDatabase();
        var redisResult = await database.ScriptEvaluateAsync(
            RegisterConnectionScript,
            [
                RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId),
                RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId),
                RedisKeys.GetUserInstancesKey(_settings.KeyPrefix, userId),
                RedisKeys.GetUserInstanceCountsKey(_settings.KeyPrefix, userId)
            ],
            [
                userId.ToString("D"),
                connectionId,
                _settings.InstanceId,
                nowUtc.ToString("O"),
                nowUtc.ToString("O"),
                checked((long)_settings.ConnectionTtl.TotalMilliseconds)
            ]);

        var scriptResult = (RedisResult[])redisResult!;
        if (scriptResult.Length != 2)
        {
            throw new InvalidOperationException("Redis registration script returned an unexpected result.");
        }

        var activeConnectionCount = checked((int)(long)scriptResult[0]);
        var isFirstConnectionForUser = (long)scriptResult[1] == 1;

        return new RealtimeConnectionMutationResult(
            userId,
            connectionId,
            activeConnectionCount,
            isFirstConnectionForUser,
            false,
            nowUtc);
    }

    public async Task<RealtimeConnectionMutationResult?> UnregisterConnectionAsync(
        Guid userId,
        string connectionId,
        string instanceId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        var database = _connectionMultiplexer.GetDatabase();
        var redisResult = await database.ScriptEvaluateAsync(
            UnregisterConnectionScript,
            [
                RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId),
                RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId),
                RedisKeys.GetUserInstancesKey(_settings.KeyPrefix, userId),
                RedisKeys.GetUserInstanceCountsKey(_settings.KeyPrefix, userId)
            ],
            [
                connectionId,
                instanceId
            ]);

        var scriptResult = (RedisResult[])redisResult!;
        if (scriptResult.Length != 3)
        {
            throw new InvalidOperationException("Redis unregistration script returned an unexpected result.");
        }

        var wasRemoved = (long)scriptResult[2] == 1;
        if (!wasRemoved)
        {
            return null;
        }

        var activeConnectionCount = checked((int)(long)scriptResult[0]);
        var isLastConnectionForUser = (long)scriptResult[1] == 1;

        return new RealtimeConnectionMutationResult(
            userId,
            connectionId,
            activeConnectionCount,
            false,
            isLastConnectionForUser,
            nowUtc);
    }
}
