namespace FlowChat.RealtimeService.Routing;

public interface IUserInstanceRoutingReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
