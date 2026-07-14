namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IRealtimeGroupManager
{
    Task AddToUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken);

    Task RemoveFromUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken);

    Task AddToConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken);

    Task RemoveFromConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken);
}
