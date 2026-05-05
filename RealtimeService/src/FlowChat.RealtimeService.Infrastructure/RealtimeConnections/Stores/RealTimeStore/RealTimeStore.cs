using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Routing;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.RealTimeStore;

internal sealed class RealTimeStore(
    IConnectionMultiplexer connectionMultiplexer,
    RealtimeConnectionsSettingsSection settings) : IRealTimeStore
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

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

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
                GetConnectionKey(connectionId),
                GetUserConnectionsKey(userId),
                RealtimeRoutingKeys.GetUserInstancesKey(_settings.KeyPrefix, userId),
                RealtimeRoutingKeys.GetUserInstanceCountsKey(_settings.KeyPrefix, userId)
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
            nowUtc);
    }

    private string GetConnectionKey(string connectionId) => $"{_settings.KeyPrefix}:connections:{connectionId}";

    private string GetUserConnectionsKey(Guid userId) => $"{_settings.KeyPrefix}:user-connections:{userId:D}";
}
