using FlowChat.Persistence.EntityFrameworkCore;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ChatMessage>(dbContext), IChatMessageWriteRepository
{
}
