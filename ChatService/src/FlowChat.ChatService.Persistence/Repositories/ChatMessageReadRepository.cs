using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageReadRepository(AppDbContext dbContext) : IChatMessageReadRepository
{
    public async Task<ConversationMessagesPageDto> GetPageBeforeAsync(
        Guid conversationId,
        int limit,
        DateTimeOffset? beforeSentAtUtc,
        Guid? beforeMessageId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ChatMessageReads
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);

        List<ChatMessageReadEntity> rows;
        if (beforeSentAtUtc.HasValue && beforeMessageId.HasValue)
        {
            var beforeUtc = beforeSentAtUtc.Value.ToUniversalTime();
            var sameTimestampRows = await query
                .Where(message => message.SentAtUtc == beforeUtc)
                .ToListAsync(cancellationToken);

            rows = sameTimestampRows
                .Where(message => message.Id.CompareTo(beforeMessageId.Value) < 0)
                .OrderByDescending(message => message.Id)
                .Take(limit + 1)
                .ToList();

            if (rows.Count < limit + 1)
            {
                var olderRows = await query
                    .Where(message => message.SentAtUtc < beforeUtc)
                    .OrderByDescending(message => message.SentAtUtc)
                    .ThenByDescending(message => message.Id)
                    .Take(limit + 1 - rows.Count)
                    .ToListAsync(cancellationToken);

                rows.AddRange(olderRows);
            }
        }
        else
        {
            if (beforeSentAtUtc.HasValue)
            {
                var beforeUtc = beforeSentAtUtc.Value.ToUniversalTime();
                query = query.Where(message => message.SentAtUtc < beforeUtc);
            }

            rows = await query
                .OrderByDescending(message => message.SentAtUtc)
                .ThenByDescending(message => message.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);
        }

        var dtos = rows
            .Select(MapToDto)
            .ToList();

        var hasMore = dtos.Count > limit;
        var items = dtos.Take(limit).ToList();
        var nextCursor = items.LastOrDefault();

        return new ConversationMessagesPageDto(
            items,
            hasMore ? nextCursor?.SentAtUtc : null,
            hasMore ? nextCursor?.Id : null,
            hasMore);
    }

    private static ChatMessageDto MapToDto(ChatMessageReadEntity message) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.SenderDisplayName,
            message.Text,
            message.SentAtUtc);
}
