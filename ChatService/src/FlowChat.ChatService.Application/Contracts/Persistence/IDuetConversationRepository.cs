namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IDuetConversationRepository
{
    Task<Guid?> FindConversationIdAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
    Task AddAsync(Guid userId1, Guid userId2, Guid conversationId, CancellationToken cancellationToken = default);
}
