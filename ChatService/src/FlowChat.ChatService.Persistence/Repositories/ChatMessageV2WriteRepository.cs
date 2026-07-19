using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageV2WriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ChatMessageV2>(dbContext), IChatMessageV2WriteRepository
{
    public override Task<ChatMessageV2?> GetByIdAsync(
        Id<ChatMessageV2> id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChatMessagesV2
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
    }

    public Task<long?> GetMaxSequenceNumAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ChatMessagesV2
            .Where(x => x.ConversationId == conversationId &&
                        x.DeletedAt == null &&
                        x.SequenceNum.HasValue)
            .MaxAsync(x => x.SequenceNum, cancellationToken);
    }
}
