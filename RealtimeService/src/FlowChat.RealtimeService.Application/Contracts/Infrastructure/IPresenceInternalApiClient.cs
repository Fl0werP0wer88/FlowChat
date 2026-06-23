namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IPresenceInternalApiClient
{
    Task InitializePresenceStatusAsync(Guid userId, CancellationToken cancellationToken);

    Task DeletePresenceStatusAsync(Guid userId, CancellationToken cancellationToken);

    Task RefreshPresenceStatusAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
