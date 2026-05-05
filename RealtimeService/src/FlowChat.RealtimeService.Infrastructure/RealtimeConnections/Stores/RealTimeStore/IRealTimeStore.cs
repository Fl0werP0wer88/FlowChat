using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.Stores.RealTimeStore;

internal interface IRealTimeStore
{
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
