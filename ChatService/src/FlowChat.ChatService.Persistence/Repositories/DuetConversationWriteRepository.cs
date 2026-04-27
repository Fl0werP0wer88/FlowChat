using FlowChat.ChatService.Application.Contracts.Persistence;
using DomainDuetConversation = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;
using DuetConversationReadModel = FlowChat.ChatService.Persistence.ReadModels.DuetConversation;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationWriteRepository(AppDbContext dbContext) : IDuetConversationWriteRepository
{
    public async Task<DomainDuetConversation> AddAsync(
        DomainDuetConversation conversation,
        CancellationToken cancellationToken = default)
    {
        var (userId1, userId2) = conversation.GetParticipantPair();
        var (first, second) = DuetConversationUserPair.Normalize(userId1, userId2);

        await dbContext.Set<DomainDuetConversation>().AddAsync(conversation, cancellationToken);

        dbContext.DuetConversations.Add(new DuetConversationReadModel
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = conversation.Id
        });

        return conversation;
    }
}
