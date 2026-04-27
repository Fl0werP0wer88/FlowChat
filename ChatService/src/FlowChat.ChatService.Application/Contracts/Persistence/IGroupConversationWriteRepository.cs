using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IGroupConversationWriteRepository
{
    Task<GroupConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupConversation> AddAsync(GroupConversation conversation, CancellationToken cancellationToken = default);
    Task UpdateAsync(GroupConversation conversation, CancellationToken cancellationToken = default);
}
