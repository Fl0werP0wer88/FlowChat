using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationReadRepository(AppDbContext dbContext) : IDuetConversationReadRepository
{
    public async Task<DuetConversationDetailDto?> GetByUserIdsAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = Normalize(requestingUserId, partnerUserId);

        var rows = await (
            from duet in dbContext.DuetConversations.AsNoTracking()
            where duet.FirstUserId == first && duet.SecondUserId == second
            from conversation in dbContext.Conversations.AsNoTracking()
                .Where(x => x.Id == duet.ConversationId)
            from participant in conversation.Participants
            join profile in dbContext.UserProfileProjections.AsNoTracking()
                on participant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new DuetConversationParticipantRow(
                duet.ConversationId.Value,
                participant.UserId,
                profile == null ? null : profile.DisplayName,
                profile == null ? null : profile.AvatarUrl,
                profile == null ? null : profile.FriendlyUserId))
            .ToListAsync(cancellationToken);

        if (rows.Count != 2)
        {
            return null;
        }

        var conversationId = rows[0].ConversationId;
        if (rows.Any(x => x.ConversationId != conversationId))
        {
            return null;
        }

        var participants = rows.ToDictionary(
            x => x.UserId,
            x => new ConversationParticipantDto(
                x.UserId,
                x.DisplayName,
                x.AvatarUrl,
                x.FriendlyUserId ?? string.Empty));

        if (!participants.TryGetValue(requestingUserId, out var requestingParticipant))
        {
            return null;
        }

        if (!participants.TryGetValue(partnerUserId, out var partnerParticipant))
        {
            return null;
        }

        return new DuetConversationDetailDto(
            conversationId,
            [requestingParticipant, partnerParticipant]);
    }

    private static (Guid First, Guid Second) Normalize(Guid userId1, Guid userId2) =>
        userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);

    private sealed record DuetConversationParticipantRow(
        Guid ConversationId,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl,
        string? FriendlyUserId);
}
