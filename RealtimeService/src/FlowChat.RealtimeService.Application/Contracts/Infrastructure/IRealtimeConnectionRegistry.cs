namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeConnectionRegistry
{
    Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

    Task<RealtimeConnectionMutationResult?> UnregisterAsync(string connectionId, CancellationToken cancellationToken);

    Task RefreshAsync(IReadOnlyCollection<RealtimeConnectionRefreshEntry> connections, CancellationToken cancellationToken);
}
