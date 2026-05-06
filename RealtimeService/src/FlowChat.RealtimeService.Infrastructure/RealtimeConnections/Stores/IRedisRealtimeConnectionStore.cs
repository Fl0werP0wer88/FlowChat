using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores;

internal interface IRedisRealtimeConnectionStore
{
    Task<Guid?> GetConnectionUserIdAsync(string connectionId);

    Task DeleteConnectionAsync(string connectionId);

    Task<bool> RefreshConnectionTtlAsync(string connectionId);

    Task<bool> RefreshUserConnectionsTtlAsync(Guid userId);

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
