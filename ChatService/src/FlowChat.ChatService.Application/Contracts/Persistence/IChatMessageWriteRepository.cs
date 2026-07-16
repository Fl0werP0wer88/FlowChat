using FlowChat.Shared.Application;
using FlowChat.ChatService.Domain.Entities.ChatMessage;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageWriteRepository : IWriteRepository<ChatMessage>
{
}

