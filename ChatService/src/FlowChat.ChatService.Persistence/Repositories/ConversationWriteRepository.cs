using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Conversation>(dbContext), IConversationWriteRepository;
