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

    Task PublishGroupConversationChangedAsync(
        Uri baseAddress,
        GroupConversationChangedParam notification,
        CancellationToken cancellationToken);

    Task PublishGroupConversationParticipantsAddedAsync(
        Uri baseAddress,
        GroupConversationParticipantsAddedParam notification,
        CancellationToken cancellationToken);

    Task PublishGroupConversationParticipantsRemovedAsync(
        Uri baseAddress,
        GroupConversationParticipantsRemovedParam notification,
        CancellationToken cancellationToken);
}
