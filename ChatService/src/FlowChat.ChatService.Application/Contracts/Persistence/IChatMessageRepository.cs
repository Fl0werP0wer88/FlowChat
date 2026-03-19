using FlowChat.ChatService.Domain.Entities;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageRepository : IAsyncRepository<ChatMessage>
{
}
