namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal interface IActiveRealtimeConnectionTracker
{
    void Track(string connectionId);

    void Untrack(string connectionId);

    IReadOnlyCollection<string> Snapshot();
}
