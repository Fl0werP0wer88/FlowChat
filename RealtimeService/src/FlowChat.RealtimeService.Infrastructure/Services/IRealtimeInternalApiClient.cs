using FlowChat.RealtimeService.Application.Realtime.Contracts;

namespace FlowChat.RealtimeService.Infrastructure.Services;

public interface IRealtimeInternalApiClient
{
    Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(PublishPresenceChangeRequest request, CancellationToken cancellationToken);
}
