using FlowChat.ChatService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationParticipantReadRepository(AppDbContext dbContext) : IConversationParticipantReadRepository
{
    public async Task<IReadOnlyCollection<Guid>?> GetParticipantUserIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await dbContext.ConversationReads
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);

        if (!conversationExists)
        {
            return null;
        }

        return await dbContext.ParticipantUserReads
            .AsNoTracking()
            .Where(participant => participant.ConversationId == conversationId)
            .Select(participant => participant.UserId)
            .ToListAsync(cancellationToken);
    }
}
