using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Routing;
using FlowChat.Shared.Infrastructure.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.UserConnectionStore;

internal sealed class RedisUserConnectionsStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeConnectionsSettingsSection settings) : IUserConnectionsStore
{
    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public Task<bool> RefreshTtlAsync(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return _redisTransactionContext.GetActiveDatabase()
            .KeyExpireAsync(RedisKeys.GetUserConnectionsKey(_settings.KeyPrefix, userId), _settings.ConnectionTtl);
    }
}
