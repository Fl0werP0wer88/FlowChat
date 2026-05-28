using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class GroupConversationReadRepository(AppDbContext dbContext) : IGroupConversationReadRepository
{
    public async Task<GroupConversationDetailDto?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var convId = Id<Conversation>.FromGuid(conversationId);

        var rawRows = await (
            from conversation in dbContext.Conversations.AsNoTracking()
            where conversation.Id == convId && conversation.Type == ConversationType.Group
            from participant in conversation.Participants
            join profile in dbContext.UserProfileProjections.AsNoTracking()
                on participant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new
            {
                ConversationId = conversation.Id.Value,
                ConversationName = conversation.Name,
                participant.UserId,
                ParticipantDisplayName = participant.DisplayName,
                ProfileFirstName = (string?) profile.FirstName,
                ProfileLastName = (string?) profile.LastName,
                ParticipantAvatarUrl = participant.AvatarUrl,
                ProfileAvatarUrl = (string?) profile.AvatarUrl
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
        return await (
            from conversation in dbContext.Conversations.AsNoTracking()
            where conversation.Type == ConversationType.Group
                  && conversation.Participants.Any(p => p.UserId == participantUserId)
            select new GroupConversationSummaryDto(
                conversation.Id.Value,
                conversation.Name!,
                conversation.Participants.Count))
            .ToListAsync(cancellationToken);
    }

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = ((string?[]) [firstName, lastName]).Where(p => !string.IsNullOrEmpty(p));
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
