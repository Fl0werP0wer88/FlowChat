using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ChatMessage>(dbContext), IChatMessageWriteRepository
{
    public Task<long?> GetMaxSequenceNumAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var typedConversationId = Id<Conversation>.FromGuid(conversationId);
        return dbContext.ChatMessages
            .Where(m => m.ConversationId == typedConversationId)
            .MaxAsync(m => (long?)m.SequenceNum, cancellationToken);
    }
}

