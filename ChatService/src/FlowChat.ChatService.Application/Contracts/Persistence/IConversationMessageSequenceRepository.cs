namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationMessageSequenceRepository
{
    Task AddAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<long> GetNextAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
