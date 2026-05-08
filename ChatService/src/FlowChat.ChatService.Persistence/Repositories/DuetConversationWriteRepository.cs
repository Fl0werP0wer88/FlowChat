using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationWriteRepository(AppDbContext dbContext) : IDuetConversationWriteRepository
{
    public async Task<DuetConversationAggregate> AddAsync(
        DuetConversationAggregate conversation,
        CancellationToken cancellationToken = default)
    {
        var (userId1, userId2) = conversation.GetParticipantPair();
        var (first, second) = DuetConversationUserPair.Normalize(userId1, userId2);

        await dbContext.Set<DuetConversationAggregate>().AddAsync(conversation, cancellationToken);

        dbContext.DuetConversations.Add(new DuetConversationLookup
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = conversation.Id
        });

        return conversation;
    }
}
