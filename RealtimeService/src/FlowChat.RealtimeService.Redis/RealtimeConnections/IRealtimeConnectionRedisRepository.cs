using FlowChat.RealtimeService.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Redis.RealtimeConnections;

public interface IRealtimeConnectionRedisRepository
{
    Task<Guid?> GetConnectionUserIdAsync(string connectionId);

    Task DeleteConnectionAsync(string connectionId);

    Task<bool> RefreshConnectionTtlAsync(string connectionId);

    Task<bool> RefreshUserConnectionsTtlAsync(Guid userId);

    Task<IReadOnlyCollection<string>> GetConnectionIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task RefreshUserInstancesTtlAsync(Guid userId);

    Task<RealtimeConnectionMutationResult> RegisterConnectionAsync(
        Guid userId,
        string connectionId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<RealtimeConnectionMutationResult?> UnregisterConnectionAsync(
        Guid userId,
        string connectionId,
        string instanceId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
