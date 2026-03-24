using FlowChat.Application.Abstractions;
using FlowChat.ChatService.Domain.Entities;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageWriteRepository : IWriteRepository<ChatMessage>
{
}
