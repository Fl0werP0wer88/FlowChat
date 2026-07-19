using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationMembershipWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ConversationMembership>(dbContext), IConversationMembershipWriteRepository
{
    public Task<ConversationMembership?> GetByConversationIdAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationMembershipsV2
            .FirstOrDefaultAsync(
                x => x.ConversationId == conversationId && x.DeletedAt == null,
                cancellationToken);
    }

    public Task<int?> GetVersionAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationMembershipsV2
            .Where(x => x.ConversationId == conversationId && x.DeletedAt == null)
            .Select(x => (int?)x.Version)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
