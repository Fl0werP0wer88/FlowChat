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

    Task PublishConversationParticipantsAddedAsync(
        Uri baseAddress,
        ConversationParticipantsAddedParam notification,
        CancellationToken cancellationToken);

    Task PublishConversationParticipantsRemovedAsync(
        Uri baseAddress,
        ConversationParticipantsRemovedParam notification,
        CancellationToken cancellationToken);
}
