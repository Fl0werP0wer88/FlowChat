namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationMessageSequenceReadRepository
{
    Task<long?> GetCurrentAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
