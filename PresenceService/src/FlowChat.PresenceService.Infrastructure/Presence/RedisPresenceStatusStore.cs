using System.Collections.Immutable;
using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FlowChat.PresenceService.Infrastructure.Presence;

internal sealed class RedisPresenceStatusStore(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<PresenceStatusSettingsSection> settings) : IPresenceStatusStore
{
    private static class HashFields
    {
        public const string UserId = "userId";
        public const string Status = "status";
        public const string ChangedAtUtc = "changedAtUtc";
    }

    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer
        ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    private readonly PresenceStatusSettingsSection _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

    public async Task<PresenceStatusSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        var values = await database.HashGetAsync(
            GetPresenceStatusKey(userId),
            [HashFields.UserId, HashFields.Status, HashFields.ChangedAtUtc]);
        if (values.All(static value => value.IsNullOrEmpty))
        {
            return null;
        }

        if (!Guid.TryParse(values[0].ToString(), out var storedUserId)
            || !Enum.TryParse<PresenceStatus>(values[1].ToString(), true, out var status)
            || !DateTimeOffset.TryParse(values[2].ToString(), out var changedAtUtc))
        {
            return null;
        }

        return new PresenceStatusSnapshot(storedUserId, status, changedAtUtc);
    }

    public async Task SetAsync(
        Guid userId,
        PresenceStatus status,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        var key = GetPresenceStatusKey(userId);
        await database.HashSetAsync(
            key,
            [
                new HashEntry(HashFields.UserId, userId.ToString("D")),
                new HashEntry(HashFields.Status, status.ToString()),
                new HashEntry(HashFields.ChangedAtUtc, changedAtUtc.ToString("O"))
            ]);
        await database.KeyExpireAsync(key, _settings.PresenceTtl);
    }

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _connectionMultiplexer.GetDatabase().KeyDeleteAsync(GetPresenceStatusKey(userId));
    }

    public Task<bool> RefreshTtlAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _connectionMultiplexer.GetDatabase().KeyExpireAsync(GetPresenceStatusKey(userId), _settings.PresenceTtl);
    }

    public async Task<IReadOnlyDictionary<Guid, PresenceStatusSnapshot>> GetManyAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0)
        {
            return ImmutableDictionary<Guid, PresenceStatusSnapshot>.Empty;
        }

        var database = _connectionMultiplexer.GetDatabase();
        var batch = database.CreateBatch();
        RedisValue[] fields = [HashFields.UserId, HashFields.Status, HashFields.ChangedAtUtc];
        var tasks = userIds
            .Select(id => (Id: id, Task: batch.HashGetAsync(GetPresenceStatusKey(id), fields)))
            .ToArray();
        batch.Execute();
        await Task.WhenAll(tasks.Select(static t => (Task)t.Task));

        var result = new Dictionary<Guid, PresenceStatusSnapshot>(tasks.Length);
        foreach (var (id, task) in tasks)
        {
            var values = task.Result;
            if (values.All(static v => v.IsNullOrEmpty))
            {
                continue;
            }

            if (!Guid.TryParse(values[0].ToString(), out var storedId)
                || !Enum.TryParse<PresenceStatus>(values[1].ToString(), true, out var status)
                || !DateTimeOffset.TryParse(values[2].ToString(), out var changedAt))
            {
                continue;
            }

            result[id] = new PresenceStatusSnapshot(storedId, status, changedAt);
        }

        return result;
    }

    private string GetPresenceStatusKey(Guid userId) => $"{_settings.KeyPrefix}:presence-status:{userId:D}";
}
