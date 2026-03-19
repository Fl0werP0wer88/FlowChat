using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageRepository(AppDbContext dbContext) : RepositoryBase<ChatMessage>(dbContext), IChatMessageRepository
{
}
