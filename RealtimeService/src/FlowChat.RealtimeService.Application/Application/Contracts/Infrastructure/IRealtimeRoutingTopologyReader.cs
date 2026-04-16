namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IRealtimeRoutingTopologyReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
