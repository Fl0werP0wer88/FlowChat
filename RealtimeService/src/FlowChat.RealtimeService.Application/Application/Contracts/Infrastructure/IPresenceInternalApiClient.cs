namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public interface IPresenceInternalApiClient
{
    Task RefreshPresenceStatusAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
