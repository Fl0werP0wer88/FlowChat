using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationWriteRepository : IWriteRepository<Conversation>
{
}
