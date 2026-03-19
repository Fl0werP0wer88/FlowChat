namespace FlowChat.RealtimeService.Worker.Services;

public interface IRealtimeInternalApiClient
{
    Task PublishMessageAsync(
        Realtime.Contracts.PublishMessageRequest request,
        CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(
        Realtime.Contracts.PublishPresenceChangeRequest request,
        CancellationToken cancellationToken);
}
