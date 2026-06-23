using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationParticipantReadRepository(AppDbContext dbContext) : IConversationParticipantReadRepository
{
    public async Task<IReadOnlyCollection<Guid>?> GetParticipantUserIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var typedConversationId = Id<Conversation>.FromGuid(conversationId);

        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Participants)
            .FirstOrDefaultAsync(conversation => conversation.Id == typedConversationId, cancellationToken);

        return conversation?.Participants.Select(participant => participant.UserId).ToList();
    }
}
