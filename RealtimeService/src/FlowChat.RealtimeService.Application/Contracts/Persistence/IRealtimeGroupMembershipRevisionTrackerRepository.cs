namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public interface IRealtimeGroupMembershipRevisionTrackerRepository
{
    Task<int?> GetRevisionAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task UpsertIfNewerAsync(
        Guid conversationId,
        int revision,
        CancellationToken cancellationToken = default);
}
