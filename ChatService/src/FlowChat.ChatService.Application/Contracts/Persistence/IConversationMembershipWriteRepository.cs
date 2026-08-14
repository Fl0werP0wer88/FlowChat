using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationMembershipWriteRepository : IWriteRepository<ConversationMembership>
{
    Task<ConversationMembership?> GetByConversationIdAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default);

    Task<int?> GetVersionAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default);
}
