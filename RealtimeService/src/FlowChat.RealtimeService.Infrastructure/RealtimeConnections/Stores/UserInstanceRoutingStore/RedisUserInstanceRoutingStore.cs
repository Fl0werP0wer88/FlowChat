using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Redis;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserInstanceRoutingStore;

internal sealed class RedisUserInstanceRoutingStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeConnectionsSettings settings)
    : IUserInstanceRoutingStore, IRealtimeRoutingTopologyReader
{
    // The routing read-model is split across a SET and HASH so reads stay cheap while unregister can still distinguish
    // "last connection on this instance" from "one of many connections on this instance"
    private const string RemoveConnectionScript = """
        local count = redis.call('HINCRBY', KEYS[2], ARGV[1], -1)
        if count <= 0 then
            redis.call('HDEL', KEYS[2], ARGV[1])
            redis.call('SREM', KEYS[1], ARGV[1])
        end

        if redis.call('HLEN', KEYS[2]) == 0 then
            redis.call('DEL', KEYS[2])
        end

        if redis.call('SCARD', KEYS[1]) == 0 then
            redis.call('DEL', KEYS[1])
        end

        return count
        """;

    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeConnectionsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public Task AddConnectionAsync(Guid userId, string instanceId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        var database = _redisTransactionContext.GetActiveDatabase();
        var userInstancesKey = GetUserInstancesKey(userId);
        var userInstanceCountsKey = GetUserInstanceCountsKey(userId);
        var incrementTask = database.HashIncrementAsync(userInstanceCountsKey, instanceId, 1);
        var addTask = database.SetAddAsync(userInstancesKey, instanceId);
        var userInstancesExpireTask = database.KeyExpireAsync(userInstancesKey, _settings.ConnectionTtl);
        var userInstanceCountsExpireTask = database.KeyExpireAsync(userInstanceCountsKey, _settings.ConnectionTtl);

        return CompleteWriteAsync(database, incrementTask, addTask, userInstancesExpireTask, userInstanceCountsExpireTask);
    }

    public async Task RemoveConnectionAsync(Guid userId, string instanceId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        // Lua keeps the count hash and routing set in sync even if concurrent unregisters hit the same user/instance pair
        await _redisTransactionContext.GetActiveDatabase().ScriptEvaluateAsync(
            RemoveConnectionScript,
            [
                GetUserInstancesKey(userId),
                GetUserInstanceCountsKey(userId)
            ],
            [instanceId]);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        cancellationToken.ThrowIfCancellationRequested();

        var filteredUserIds = userIds
            .Where(static userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
        if (filteredUserIds.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<string>>();
        }

        var database = _redisTransactionContext.GetActiveDatabase();
        var instanceTasks = filteredUserIds.ToDictionary(
            userId => userId,
            userId => database.SetMembersAsync(GetUserInstancesKey(userId)));

        await Task.WhenAll(instanceTasks.Values.Cast<Task>());

        Dictionary<Guid, IReadOnlyCollection<string>> instancesByUser = [];
        foreach (var (userId, task) in instanceTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var instanceIds = task.Result
                .Select(static value => value.ToString())
                .Where(static instanceId => !string.IsNullOrWhiteSpace(instanceId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (instanceIds.Length == 0)
            {
                continue;
            }

            instancesByUser[userId] = instanceIds;
        }

        return instancesByUser;
    }

    public Task RefreshTtlAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        var database = _redisTransactionContext.GetActiveDatabase();
        var userInstancesExpireTask = database.KeyExpireAsync(GetUserInstancesKey(userId), _settings.ConnectionTtl);
        var userInstanceCountsExpireTask = database.KeyExpireAsync(GetUserInstanceCountsKey(userId), _settings.ConnectionTtl);

        return CompleteWriteAsync(database, userInstancesExpireTask, userInstanceCountsExpireTask);
    }

    private string GetUserInstancesKey(Guid userId) => $"{_settings.KeyPrefix}:user-instances:{userId:D}";

    private string GetUserInstanceCountsKey(Guid userId) => $"{_settings.KeyPrefix}:user-instance-counts:{userId:D}";

    private static Task CompleteWriteAsync(IDatabaseAsync database, params Task[] operations) =>
        database is ITransaction ? Task.CompletedTask : Task.WhenAll(operations);
}
