namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public interface IRealtimeGroupMembershipVersionTrackerRepository
{
    Task<int?> GetVersionAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task UpsertIfNewerAsync(
        Guid conversationId,
        int version,
        CancellationToken cancellationToken = default);
}
