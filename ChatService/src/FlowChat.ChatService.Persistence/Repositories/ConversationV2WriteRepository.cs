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

    public async Task<ConversationV2> AddAsync(
        ConversationV2 conversation,
        IReadOnlyCollection<Id<UserProfileMarker>> initialParticipantUserIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(initialParticipantUserIds);

        await dbContext.ConversationsV2.AddAsync(conversation, cancellationToken);
        await dbContext.ConversationMessageSequencesV2.AddAsync(
            new ConversationMessageSequenceEntityV2 { ConversationId = conversation.Id },
            cancellationToken);

        if (conversation.ConversationType == ConversationType.Duet)
        {
            if (initialParticipantUserIds.Count != 2)
            {
                throw new ArgumentException(
                    "A duet lookup requires exactly two participants.",
                    nameof(initialParticipantUserIds));
            }

            var normalized = initialParticipantUserIds
                .Select(x => x.Value)
                .Order()
                .ToArray();

            await dbContext.DuetConversationsV2.AddAsync(
                new DuetConversationLookupEntityV2
                {
                    ConversationId = conversation.Id,
                    FirstUserId = normalized[0],
                    SecondUserId = normalized[1]
                },
                cancellationToken);
        }

        return conversation;
    }

    public async Task<ConversationV2?> GetDuetByUserIdsAsync(
        Id<UserProfileMarker> firstUserId,
        Id<UserProfileMarker> secondUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = new[] { firstUserId.Value, secondUserId.Value }
            .Order()
            .ToArray();

        var conversationId = await dbContext.DuetConversationsV2
            .Where(x => x.FirstUserId == normalized[0] &&
                        x.SecondUserId == normalized[1] &&
                        x.DeletedAt == null)
            .Select(x => x.ConversationId)
            .SingleOrDefaultAsync(cancellationToken);

        return conversationId is null
            ? null
            : await GetByIdAsync(conversationId, cancellationToken);
    }
}
