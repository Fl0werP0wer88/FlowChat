using FlowChat.Shared.Persistance;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ChatMessage>(dbContext), IChatMessageWriteRepository
{
}

