namespace FlowChat.RealtimeService.Routing;

public interface IRealtimeRoutingTopologyReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
