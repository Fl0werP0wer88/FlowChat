using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<DuetConversation, Conversation>(dbContext), IDuetConversationWriteRepository
{
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
