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

        var rows = await (
            from conversation in dbContext.Conversations.AsNoTracking()
            where conversation.Id == convId && conversation.Type == ConversationType.Group
            from participant in conversation.Participants
            join profile in dbContext.UserProfileProjections.AsNoTracking()
                on participant.UserId equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new GroupConversationParticipantRow(
                conversation.Id.Value,
                conversation.Name,
                participant.UserId,
                string.IsNullOrEmpty(participant.DisplayName)
                    ? (profile == null ? null : profile.DisplayName)
                    : participant.DisplayName,
                string.IsNullOrEmpty(participant.AvatarUrl)
                    ? (profile == null ? null : profile.AvatarUrl)
                    : participant.AvatarUrl))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return null;

        var participants = rows
            .Select(r => new ConversationParticipantDto(r.UserId, r.DisplayName, r.AvatarUrl, r.UserId))
            .ToList();

        return new GroupConversationDetailDto(rows[0].ConversationId, rows[0].Name!, participants);
    }

    private sealed record GroupConversationParticipantRow(
        Guid ConversationId,
        string? Name,
        Guid UserId,
        string? DisplayName,
        string? AvatarUrl);
}
