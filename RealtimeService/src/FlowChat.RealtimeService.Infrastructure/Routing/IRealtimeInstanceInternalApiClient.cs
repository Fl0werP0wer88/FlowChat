using FlowChat.RealtimeService.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public interface IRealtimeInstanceInternalApiClient
{
    Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageParam notification,
        CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PresenceChangedParam notification,
        CancellationToken cancellationToken);
}
