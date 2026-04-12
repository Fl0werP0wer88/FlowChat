using System.Collections.Concurrent;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class InMemoryActiveRealtimeConnectionTracker : IActiveRealtimeConnectionTracker
{
    private readonly ConcurrentDictionary<string, RealtimeConnectionRefreshEntry> _connections = new(StringComparer.Ordinal);

    public void Track(Guid userId, string connectionId, UserPresenceStatus status)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        _connections[connectionId] = new RealtimeConnectionRefreshEntry(userId, connectionId, status);
    }

    public void Untrack(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        _connections.TryRemove(connectionId, out _);
    }

    public bool TryGet(string connectionId, out RealtimeConnectionRefreshEntry? connection)
    {
        if (_connections.TryGetValue(connectionId, out var trackedConnection))
        {
            connection = trackedConnection;
            return true;
        }

        connection = null;
        return false;
    }

    public IReadOnlyCollection<RealtimeConnectionRefreshEntry> Snapshot() => _connections.Values.ToArray();
}
