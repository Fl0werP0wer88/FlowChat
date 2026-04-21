namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IDuetConversationWriteRepository
{
    Task AddAsync(Guid userId1, Guid userId2, Guid conversationId, CancellationToken cancellationToken = default);
}
