using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<DuetConversation, Conversation>(dbContext), IDuetConversationWriteRepository
{
    public override async Task<DuetConversation?> GetByIdAsync(
        Id<DuetConversation> id,
        CancellationToken cancellationToken = default)
    {
        var conversationId = Id<Conversation>.FromId(id);

        return await DbContext.Set<DuetConversation>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
    }

    public override async Task<DuetConversation> AddAsync(
        DuetConversation conversation,
        CancellationToken cancellationToken = default)
    {
        var (userId1, userId2) = conversation.GetParticipantPair();
        var (first, second) = DuetConversationUserPair.Normalize(userId1.Value, userId2.Value);

        await DbContext.Set<DuetConversation>().AddAsync(conversation, cancellationToken);

        dbContext.DuetConversations.Add(new DuetConversationLookupEntity
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = conversation.Id
        });

        return conversation;
    }
}
