using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationParticipantWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ConversationParticipant>(dbContext), IConversationParticipantWriteRepository
{
    public Task<ConversationParticipant?> GetActiveAsync(
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationParticipantsV2
            .FirstOrDefaultAsync(
                x => x.ConversationId == conversationId &&
                     x.UserId == userId &&
                     x.DeletedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConversationParticipant>> GetActiveByUserIdsAsync(
        Id<ConversationV2> conversationId,
        IReadOnlyCollection<Id<UserProfileMarker>> userIds,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ConversationParticipantsV2
            .Where(x => x.ConversationId == conversationId &&
                        userIds.Contains(x.UserId) &&
                        x.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConversationParticipant>> GetActiveByConversationIdAsync(
        Id<ConversationV2> conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ConversationParticipantsV2
            .Where(x => x.ConversationId == conversationId && x.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConversationParticipant>> GetHiddenByConversationIdAsync(
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> excludedUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ConversationParticipantsV2
            .Where(x => x.ConversationId == conversationId &&
                        x.UserId != excludedUserId &&
                        x.IsHidden &&
                        x.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }
}
