using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationMessageSequenceRepositoryV2
{
    Task<long> GetNextAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default);
}
