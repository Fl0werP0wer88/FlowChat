namespace FlowChat.RealtimeService.Redis.Routing;

public interface IUserInstanceRoutingReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
