using System.Collections.Concurrent;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal sealed class InMemoryActiveRealtimeConnectionTracker : IActiveRealtimeConnectionTracker
{
    private readonly ConcurrentDictionary<string, byte> _connectionIds = new(StringComparer.Ordinal);

    public void Track(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        _connectionIds[connectionId] = 0;
    }

    public void Untrack(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        _connectionIds.TryRemove(connectionId, out _);
    }

    public IReadOnlyCollection<string> Snapshot() => _connectionIds.Keys.ToArray();
}
