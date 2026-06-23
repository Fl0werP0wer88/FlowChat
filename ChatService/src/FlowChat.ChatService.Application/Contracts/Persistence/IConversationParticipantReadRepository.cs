namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationParticipantReadRepository
{
    Task<IReadOnlyCollection<Guid>?> GetParticipantUserIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
