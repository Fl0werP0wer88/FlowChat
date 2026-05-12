using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public interface IRealtimeInstanceInternalApiClient
{
    Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageNotification notification,
        CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PresenceChangedNotification notification,
        CancellationToken cancellationToken);
}
