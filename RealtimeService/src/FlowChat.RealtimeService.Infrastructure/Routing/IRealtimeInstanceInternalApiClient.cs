using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public interface IRealtimeInstanceInternalApiClient
{
    Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageNotification notification,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken);

    Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PresenceChangedNotification notification,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken);
}
