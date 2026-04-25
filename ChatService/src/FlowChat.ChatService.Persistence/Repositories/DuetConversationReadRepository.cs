using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationReadRepository(AppDbContext dbContext) : IDuetConversationReadRepository
{
    public async Task<Guid?> FindConversationIdAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = DuetConversationUserPair.Normalize(userId1, userId2);

        var entry = await dbContext.DuetConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FirstUserId == first && x.SecondUserId == second, cancellationToken);

        return entry?.ConversationId.Value;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> FindConversationIdsByPartnerIdsAsync(
        Guid requestingUserId,
        IEnumerable<Guid> partnerUserIds,
        CancellationToken cancellationToken = default)
    {
        var ids = partnerUserIds.ToList();

        return await dbContext.DuetConversations
            .AsNoTracking()
            .Where(x =>
                (x.FirstUserId == requestingUserId && ids.Contains(x.SecondUserId)) ||
                (x.SecondUserId == requestingUserId && ids.Contains(x.FirstUserId)))
            .Select(x => new
            {
                PartnerUserId = x.FirstUserId == requestingUserId ? x.SecondUserId : x.FirstUserId,
                ConversationId = x.ConversationId.Value
            })
            .ToDictionaryAsync(x => x.PartnerUserId, x => x.ConversationId, cancellationToken);
    }

    public async Task<DuetConversationDetailDto?> GetByUserIdsAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = DuetConversationUserPair.Normalize(requestingUserId, partnerUserId);

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

    private sealed record DuetConversationParticipantRow(
        Guid ConversationId,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl,
        string? FriendlyUserId);
}
