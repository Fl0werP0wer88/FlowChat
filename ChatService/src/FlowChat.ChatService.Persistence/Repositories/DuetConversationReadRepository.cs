using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationReadRepository(AppDbContext dbContext) : ReadRepositoryBase, IDuetConversationReadRepository
{
    private const int DuetConversationType = 1;

    public async Task<DuetConversationDetailDto?> GetByUserIdsAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = DuetConversationUserPair.Normalize(requestingUserId, partnerUserId);

        var rawRows = await (
            from conversation in Active(dbContext.ConversationReadsV2)
            where conversation.DuetFirstUserId == first && conversation.DuetSecondUserId == second
            join participant in Active(dbContext.ConversationParticipantReadsV2)
                on conversation.Id equals participant.ConversationId
            join profile in Active(dbContext.UserProfileProjections)
                on participant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new
            {
                ConversationId = conversation.Id,
                participant.UserId,
                ParticipantDisplayName = participant.DisplayName,
                ProfileFirstName = (string?)profile.FirstName,
                ProfileLastName = (string?)profile.LastName,
                ProfileAvatarUrl = (string?)profile.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        var rows = rawRows.Select(r => new DuetConversationParticipantRow(
                r.ConversationId,
                r.UserId,
                string.IsNullOrEmpty(r.ParticipantDisplayName)
                    ? ComputeDisplayName(r.ProfileFirstName, r.ProfileLastName)
                    : r.ParticipantDisplayName,
                r.ProfileAvatarUrl))
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
    //ToDo: Pomyśleć o uproszczeniu zapytania albo robic read model
    public async Task<IReadOnlyCollection<ContactDto>> GetContactsForUserAsync(
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var currentSequences = dbContext.ConversationMessageSequenceReadsV2
            .AsNoTracking()
            .Select(sequence => new
            {
                sequence.ConversationId,
                CurrentMsgSeqNum = (long?)sequence.LastAssignedSequenceNum
            });

        var rawRows = await (
            from conversation in Active(dbContext.ConversationReadsV2)
            join myParticipant in Active(dbContext.ConversationParticipantReadsV2)
                on conversation.Id equals myParticipant.ConversationId
            where conversation.ConversationType == DuetConversationType
                  && myParticipant.UserId == requestingUserId
                  && !myParticipant.IsHidden
                  && myParticipant.DuetPartnerUserId != null
            join partnerParticipant in Active(dbContext.ConversationParticipantReadsV2)
                on new
                {
                    myParticipant.ConversationId,
                    UserId = myParticipant.DuetPartnerUserId
                }
                equals new
                {
                    partnerParticipant.ConversationId,
                    UserId = (Guid?)partnerParticipant.UserId
                }
            join sequence in currentSequences
                on conversation.Id equals sequence.ConversationId into sequenceGroup
            from sequence in sequenceGroup.DefaultIfEmpty()
            join profile in Active(dbContext.UserProfileProjections)
                on partnerParticipant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new
            {
                ConversationId = conversation.Id,
                PartnerUserId = partnerParticipant.UserId,
                PartnerDisplayName = partnerParticipant.DisplayName,
                ProfileFirstName = (string?)profile.FirstName,
                ProfileLastName = (string?)profile.LastName,
                ProfileAvatarUrl = (string?)profile.AvatarUrl,
                ProfileEmail = (string?)profile.Email,
                LastReadMsgSeqNum = myParticipant.LastReadMessageSequenceNum,
                CurrentMsgSeqNum = sequence.CurrentMsgSeqNum ?? 0,
                IsBlocked = myParticipant.IsBlocked,
                IsBlockedByPartner = partnerParticipant.IsBlocked,
                IsMuted = myParticipant.IsMuted,
                IsHidden = myParticipant.IsHidden
            })
            .ToListAsync(cancellationToken);

        return rawRows.Select(r => new ContactDto(
                r.PartnerUserId,
                string.IsNullOrEmpty(r.PartnerDisplayName)
                    ? ComputeDisplayName(r.ProfileFirstName, r.ProfileLastName)
                    : r.PartnerDisplayName,
                r.ProfileAvatarUrl,
                r.ProfileEmail,
                r.ConversationId,
                r.LastReadMsgSeqNum,
                r.CurrentMsgSeqNum,
                r.IsBlocked,
                r.IsBlockedByPartner,
                r.IsMuted,
                r.IsHidden))
            .ToList();
    }

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = ((string?[])[firstName, lastName]).Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private sealed record DuetConversationParticipantRow(
        Guid ConversationId,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl);
}
