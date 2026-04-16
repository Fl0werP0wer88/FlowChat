namespace FlowChat.RealtimeService.Consumers.Services;

public interface IRealtimeInternalApiClient
{
    Task PublishMessageAsync(Uri baseAddress, Realtime.Contracts.PublishMessageRequest request, CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(
        Uri baseAddress,
        Realtime.Contracts.PublishPresenceChangeRequest request,
        CancellationToken cancellationToken);
}
