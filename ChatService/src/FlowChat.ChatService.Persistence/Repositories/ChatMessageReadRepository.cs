using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageReadRepository(AppDbContext dbContext) : ReadRepositoryBase, IChatMessageReadRepository
{
    public async Task<IReadOnlyCollection<ChatMessageDto>> GetBeforeSequenceAsync(
        Guid conversationId,
        long throughSequenceNum,
        long? beforeSequenceNum,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = Active(dbContext.ChatMessageReadsV2)
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SequenceNum <= throughSequenceNum);

        if (beforeSequenceNum.HasValue)
        {
            query = query.Where(message => message.SequenceNum < beforeSequenceNum.Value);
        }

        var rows = await query
            .OrderByDescending(message => message.SequenceNum)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetAfterSequenceAsync(
        Guid conversationId,
        long afterSequenceNum,
        long throughSequenceNum,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await Active(dbContext.ChatMessageReadsV2)
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SequenceNum > afterSequenceNum &&
                message.SequenceNum <= throughSequenceNum)
            .OrderBy(message => message.SequenceNum)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto).ToList();
    }

    private static ChatMessageDto MapToDto(ChatMessageReadEntityV2 message) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.Text,
            message.SentAtUtc,
            message.SequenceNum);
}
