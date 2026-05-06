using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Routing;
using FlowChat.Shared.Infrastructure.Redis;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.ConnectionStore;

internal sealed class RedisConnectionStore(
    IRedisTransactionContext redisTransactionContext,
    RealtimeConnectionsSettingsSection settings) : IConnectionStore
{
    private static class HashFields
    {
        public const string UserId = "userId";
    }

    private readonly IRedisTransactionContext _redisTransactionContext = redisTransactionContext
        ?? throw new ArgumentNullException(nameof(redisTransactionContext));
    private readonly RealtimeConnectionsSettingsSection _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<Guid?> GetUserIdAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var userIdValue = await _redisTransactionContext.GetActiveDatabase()
            .HashGetAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId), HashFields.UserId);
        if (userIdValue.IsNullOrEmpty || !Guid.TryParse(userIdValue.ToString(), out var userId) || userId == Guid.Empty)
        {
            return null;
        }

        return userId;
    }

    public Task DeleteAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _redisTransactionContext.GetActiveDatabase()
            .KeyDeleteAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId));
    }

    public Task<bool> RefreshTtlAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return _redisTransactionContext.GetActiveDatabase()
            .KeyExpireAsync(RedisKeys.GetConnectionKey(_settings.KeyPrefix, connectionId), _settings.ConnectionTtl);
    }
}
