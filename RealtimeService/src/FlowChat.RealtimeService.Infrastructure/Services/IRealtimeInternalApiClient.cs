using FlowChat.RealtimeService.Application.Realtime.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Services;

public interface IRealtimeInternalApiClient
{
    Task ForwardReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken);

    Task ForwardPresenceChangedAsync(PresenceChangedRequest request, CancellationToken cancellationToken);
}
