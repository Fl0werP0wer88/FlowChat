using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationParticipantReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase, IConversationParticipantReadRepository
{
    public async Task<IReadOnlyCollection<Guid>?> GetParticipantUserIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await Active(dbContext.ConversationReads)
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);

        if (!conversationExists)
        {
            return null;
        }

        return await Active(dbContext.ParticipantUserReads)
            .Where(participant => participant.ConversationId == conversationId)
            .Select(participant => participant.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParticipantStateDto>?> GetParticipantStatesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await Active(dbContext.ConversationReads)
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);

        if (!conversationExists)
        {
            return null;
        }

        return await Active(dbContext.ParticipantUserReads)
            .Where(participant => participant.ConversationId == conversationId)
            .Select(participant => new ParticipantStateDto(participant.UserId, participant.IsBlocked, participant.IsHidden))
            .ToListAsync(cancellationToken);
    }
}
