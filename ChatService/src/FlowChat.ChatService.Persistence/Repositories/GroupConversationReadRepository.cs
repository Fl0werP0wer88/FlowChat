using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class GroupConversationReadRepository(AppDbContext dbContext) : ReadRepositoryBase, IGroupConversationReadRepository
{
    private const int GroupConversationType = 2;

    public async Task<GroupConversationDetailDto?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var rawRows = await (
            from conversation in Active(dbContext.ConversationReadsV2)
            where conversation.Id == conversationId && conversation.ConversationType == GroupConversationType
            join participant in Active(dbContext.ConversationParticipantReadsV2)
                on conversation.Id equals participant.ConversationId
            join profile in Active(dbContext.UserProfileProjections)
                on participant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new
            {
                ConversationId = conversation.Id,
                ConversationName = conversation.Name,
                participant.UserId,
                ParticipantDisplayName = participant.DisplayName,
                ProfileFirstName = (string?)profile.FirstName,
                ProfileLastName = (string?)profile.LastName,
                ProfileAvatarUrl = (string?)profile.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        var rows = rawRows.Select(r => new GroupConversationParticipantRow(
            r.ConversationId,
            r.ConversationName,
            r.UserId,
            string.IsNullOrEmpty(r.ParticipantDisplayName)
                ? ComputeDisplayName(r.ProfileFirstName, r.ProfileLastName)
                : r.ParticipantDisplayName,
            r.ProfileAvatarUrl))
            .ToList();

        if (rows.Count == 0)
            return null;

        var participants = rows
            .Select(r => new ConversationParticipantDto(r.UserId, r.DisplayName, r.AvatarUrl, r.UserId))
            .ToList();

        return new GroupConversationDetailDto(rows[0].ConversationId, rows[0].Name!, participants);
    }

    public async Task<IReadOnlyCollection<GroupConversationSummaryDto>> GetByParticipantUserIdAsync(
        Guid participantUserId,
        CancellationToken cancellationToken = default)
    {
        //ToDo: Rozważyć przerzucenie tego do oddzielnego ReadModelu zamiast robić joiny. Będzie też można pozbyć się wtedy części indeksów.
        var activeParticipants = Active(dbContext.ConversationParticipantReadsV2);
        var currentSequences = Active(dbContext.ChatMessageReadsV2)
            .Where(message => message.SequenceNum.HasValue)
            .GroupBy(message => message.ConversationId)
            .Select(messages => new
            {
                ConversationId = messages.Key,
                CurrentMsgSeqNum = messages.Max(message => message.SequenceNum)
            });

        return await (
            from conversation in Active(dbContext.ConversationReadsV2)
            join participant in activeParticipants
                on conversation.Id equals participant.ConversationId
            where conversation.ConversationType == GroupConversationType
                  && participant.UserId == participantUserId
            join sequence in currentSequences
                on conversation.Id equals sequence.ConversationId into sequenceGroup
            from sequence in sequenceGroup.DefaultIfEmpty()
            select new GroupConversationSummaryDto(
                conversation.Id,
                conversation.Name!,
                activeParticipants.Count(x => x.ConversationId == conversation.Id),
                participant.LastReadMessageSequenceNum,
                sequence.CurrentMsgSeqNum ?? 0))
            .ToListAsync(cancellationToken);
    }

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = ((string?[])[firstName, lastName]).Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private sealed record GroupConversationParticipantRow(
        Guid ConversationId,
        string? Name,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl);
}
