using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Infrastructure.Configuration;
using StackExchange.Redis;

namespace FlowChat.PresenceService.Infrastructure.Presence;

internal sealed class RedisPresenceStatusStore(
    IConnectionMultiplexer connectionMultiplexer,
    PresenceStatusSettings settings) : IPresenceStatusStore
{
    private static class HashFields
    {
        public const string UserId = "userId";
        public const string Status = "status";
        public const string ChangedAtUtc = "changedAtUtc";
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly PresenceStatusSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<PresenceStatusSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        var values = await database.HashGetAsync(
            GetUserStatusKey(userId),
            [HashFields.UserId, HashFields.Status, HashFields.ChangedAtUtc]);
        if (values.All(static value => value.IsNullOrEmpty))
        {
            return null;
        }

        if (!Guid.TryParse(values[0].ToString(), out var storedUserId)
            || !Enum.TryParse<UserStatus>(values[1].ToString(), true, out var status)
            || !DateTimeOffset.TryParse(values[2].ToString(), out var changedAtUtc))
        {
            return null;
        }

        return new PresenceStatusSnapshot(storedUserId, status, changedAtUtc);
    }

    public async Task SetAsync(
        Guid userId,
        UserStatus status,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        await database.HashSetAsync(
            GetUserStatusKey(userId),
            [
                new HashEntry(HashFields.UserId, userId.ToString("D")),
                new HashEntry(HashFields.Status, status.ToString()),
                new HashEntry(HashFields.ChangedAtUtc, changedAtUtc.ToString("O"))
            ]);
    }

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _connectionMultiplexer.GetDatabase().KeyDeleteAsync(GetUserStatusKey(userId));
    }

    private string GetUserStatusKey(Guid userId) => $"{_settings.KeyPrefix}:user-status:{userId:D}";
}
