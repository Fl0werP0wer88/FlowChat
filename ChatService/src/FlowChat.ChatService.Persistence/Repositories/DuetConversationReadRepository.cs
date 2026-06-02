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

        var participantRows = await (
            from duet in dbContext.DuetConversations.AsNoTracking()
            where duet.FirstUserId == first && duet.SecondUserId == second
            from conversation in dbContext.Conversations.AsNoTracking()
                .Where(x => x.Id == duet.ConversationId)
            from participant in conversation.Participants
            select new
            {
                ConversationId = duet.ConversationId.Value,
                UserId = participant.UserId.Value,
                ParticipantDisplayName = participant.DisplayName,
                ParticipantAvatarUrl = participant.AvatarUrl,
            })
            .ToListAsync(cancellationToken);

        var userIds = participantRows.Select(x => x.UserId).ToList();
        var profiles = await dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(x => !x.IsDeleted && userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var rows = participantRows.Select(r =>
        {
            profiles.TryGetValue(r.UserId, out var profile);

            return new DuetConversationParticipantRow(
            r.ConversationId,
            r.UserId,
            string.IsNullOrEmpty(r.ParticipantDisplayName)
                ? ComputeDisplayName(profile?.FirstName, profile?.LastName)
                : r.ParticipantDisplayName,
            string.IsNullOrEmpty(r.ParticipantAvatarUrl)
                ? profile?.AvatarUrl
                : r.ParticipantAvatarUrl);
        })
            .ToList();

        if (rows.Count != 2)
        {
            return null;
        }

        var conversationId = rows[0].ConversationId;

        var participants = rows.ToDictionary(
            x => x.UserId,
            x => new ConversationParticipantDto(
                x.UserId,
                x.DisplayName,
                x.AvatarUrl,
                x.UserId));

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

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = ((string?[]) [firstName, lastName]).Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private sealed record DuetConversationParticipantRow(
        Guid ConversationId,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl);
}
