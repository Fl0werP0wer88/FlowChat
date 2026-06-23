using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
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
        var typedConversationId = Id<Conversation>.FromGuid(conversationId);
        var query = dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == typedConversationId);

        List<ChatMessage> rows;
        if (beforeSentAtUtc.HasValue && beforeMessageId.HasValue)
        {
            var beforeUtc = UtcDateTimeOffset.Create(beforeSentAtUtc.Value);
            var typedBeforeMessageId = Id<ChatMessage>.FromGuid(beforeMessageId.Value);
            var sameTimestampRows = await query
                .Where(message => message.SentAtUtc == beforeUtc)
                .ToListAsync(cancellationToken);

            rows = sameTimestampRows
                .Where(message => message.Id.Value.CompareTo(typedBeforeMessageId.Value) < 0)
                .OrderByDescending(message => message.Id.Value)
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
                var beforeUtc = UtcDateTimeOffset.Create(beforeSentAtUtc.Value);
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

    private static ChatMessageDto MapToDto(ChatMessage message) =>
        new(
            message.Id.Value,
            message.ConversationId.Value,
            message.SenderUserId,
            message.SenderDisplayName,
            message.Text,
            message.SentAtUtc.Value);
}
