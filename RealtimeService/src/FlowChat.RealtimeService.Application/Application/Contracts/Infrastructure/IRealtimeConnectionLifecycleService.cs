namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeConnectionLifecycleService
{
    Task RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken);

    Task UnregisterAsync(string connectionId, CancellationToken cancellationToken);
}
