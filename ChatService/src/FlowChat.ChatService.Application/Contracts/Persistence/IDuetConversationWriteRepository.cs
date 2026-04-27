using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IDuetConversationWriteRepository
{
    Task<DuetConversation> AddAsync(
        DuetConversation conversation,
        CancellationToken cancellationToken = default);
}
