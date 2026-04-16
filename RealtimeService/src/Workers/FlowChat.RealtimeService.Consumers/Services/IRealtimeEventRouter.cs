using FlowChat.RealtimeService.Consumers.Realtime.Contracts;

namespace FlowChat.RealtimeService.Consumers.Services;

public interface IRealtimeEventRouter
{
    Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(PublishPresenceChangeRequest request, CancellationToken cancellationToken);
}
