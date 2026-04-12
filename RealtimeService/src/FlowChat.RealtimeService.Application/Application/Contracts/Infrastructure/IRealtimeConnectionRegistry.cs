namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeConnectionRegistry
{
    Task RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

    Task UnregisterAsync(string connectionId, CancellationToken cancellationToken);

    Task RefreshAsync(IReadOnlyCollection<RealtimeConnectionRefreshEntry> connections, CancellationToken cancellationToken);
}
