using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationV2WriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<ConversationV2>(dbContext), IConversationV2WriteRepository
{
    public override Task<ConversationV2?> GetByIdAsync(
        Id<ConversationV2> id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationsV2
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
    }

    public override async Task<ConversationV2> AddAsync(
        ConversationV2 conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        await dbContext.ConversationsV2.AddAsync(conversation, cancellationToken);
        await dbContext.ConversationMessageSequencesV2.AddAsync(
            new ConversationMessageSequenceEntityV2 { ConversationId = conversation.Id },
            cancellationToken);

        return conversation;
    }

    public async Task<ConversationV2?> GetDuetByUserIdsAsync(
        Id<UserProfileMarker> firstUserId,
        Id<UserProfileMarker> secondUserId,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = DuetConversationUserPair.Normalize(firstUserId.Value, secondUserId.Value);
        var normalizedFirstUserId = Id<UserProfileMarker>.FromGuid(first);
        var normalizedSecondUserId = Id<UserProfileMarker>.FromGuid(second);

        return await dbContext.ConversationsV2
            .SingleOrDefaultAsync(
                conversation =>
                    conversation.DuetParticipants != null &&
                    conversation.DuetParticipants.FirstUserId == normalizedFirstUserId &&
                    conversation.DuetParticipants.SecondUserId == normalizedSecondUserId &&
                    conversation.DeletedAt == null,
                cancellationToken);
    }
}
