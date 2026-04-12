using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.RealtimeConnections;

internal interface IActiveRealtimeConnectionTracker
{
    void Track(Guid userId, string connectionId, UserStatus status);

    void Untrack(string connectionId);

    bool TryGet(string connectionId, out RealtimeConnectionRefreshEntry? connection);

    IReadOnlyCollection<RealtimeConnectionRefreshEntry> Snapshot();
}
