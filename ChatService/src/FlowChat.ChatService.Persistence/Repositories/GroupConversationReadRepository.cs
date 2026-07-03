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
            from conversation in Active(dbContext.ConversationReads)
            where conversation.Id == conversationId && conversation.Type == GroupConversationType
            join participant in Active(dbContext.ParticipantUserReads)
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
                ParticipantAvatarUrl = participant.AvatarUrl,
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
            string.IsNullOrEmpty(r.ParticipantAvatarUrl)
                ? r.ProfileAvatarUrl
                : r.ParticipantAvatarUrl))
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
        var activeParticipants = Active(dbContext.ParticipantUserReads);

        return await (
            from conversation in Active(dbContext.ConversationReads)
            join participant in activeParticipants
                on conversation.Id equals participant.ConversationId
            where conversation.Type == GroupConversationType
                  && participant.UserId == participantUserId
            select new GroupConversationSummaryDto(
                conversation.Id,
                conversation.Name!,
                activeParticipants.Count(x => x.ConversationId == conversation.Id),
                participant.LastReadMessageSequenceNum,
                conversation.LastMsgSequenceNum))
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
