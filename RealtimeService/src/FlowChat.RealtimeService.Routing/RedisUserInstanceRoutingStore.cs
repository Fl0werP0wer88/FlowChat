using FlowChat.RealtimeService.Routing.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Redis;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Routing;

public sealed class RedisUserInstanceRoutingStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeRoutingSettingsSection settings)
    : IUserInstanceRoutingStore, IUserInstanceRoutingReader
{
    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeRoutingSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

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
            userId => database.SetMembersAsync(RedisKeys.GetUserInstancesKey(_settings.KeyPrefix, userId)));

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
        var userInstancesExpireTask = database.KeyExpireAsync(
            RedisKeys.GetUserInstancesKey(_settings.KeyPrefix, userId),
            _settings.ConnectionTtl);
        var userInstanceCountsExpireTask = database.KeyExpireAsync(
            RedisKeys.GetUserInstanceCountsKey(_settings.KeyPrefix, userId),
            _settings.ConnectionTtl);

        return CompleteWriteAsync(database, userInstancesExpireTask, userInstanceCountsExpireTask);
    }

    private static Task CompleteWriteAsync(IDatabaseAsync database, params Task[] operations) =>
        database is ITransaction ? Task.CompletedTask : Task.WhenAll(operations);
}
