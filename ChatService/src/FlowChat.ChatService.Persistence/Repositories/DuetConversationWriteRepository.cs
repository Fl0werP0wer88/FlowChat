using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.ReadModels;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationWriteRepository(AppDbContext dbContext) : IDuetConversationWriteRepository
{
    public Task AddAsync(Guid userId1, Guid userId2, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var (first, second) = DuetConversationUserPair.Normalize(userId1, userId2);

        dbContext.DuetConversations.Add(new DuetConversation
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = Id<Conversation>.FromGuid(conversationId)
        });

        return Task.CompletedTask;
    }
}
