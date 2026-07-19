using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageV2WriteRepository : IWriteRepository<ChatMessageV2>
{
    Task<long?> GetMaxSequenceNumAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default);
}
