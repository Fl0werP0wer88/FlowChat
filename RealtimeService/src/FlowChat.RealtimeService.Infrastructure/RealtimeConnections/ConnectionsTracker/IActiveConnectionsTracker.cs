using FlowChat.RealtimeService.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections.ConnectionsTracker;

internal interface IActiveConnectionsTracker
{
    void Track(Guid userId, string connectionId);

    void Untrack(string connectionId);

    bool TryGet(string connectionId, out RealtimeConnectionRefreshEntry? connection);

    IReadOnlyCollection<RealtimeConnectionRefreshEntry> Snapshot();
}
