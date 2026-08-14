using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ChatMessageReadRepository(AppDbContext dbContext) : ReadRepositoryBase, IChatMessageReadRepository
{
    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRangeDescendingAsync(
        Guid conversationId,
        long startSequenceNum,
        long endSequenceNum,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await Active(dbContext.ChatMessageReadsV2)
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SequenceNum >= startSequenceNum &&
                message.SequenceNum <= endSequenceNum)
            .OrderByDescending(message => message.SequenceNum)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRangeAscendingAsync(
        Guid conversationId,
        long startSequenceNum,
        long endSequenceNum,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await Active(dbContext.ChatMessageReadsV2)
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SequenceNum >= startSequenceNum &&
                message.SequenceNum <= endSequenceNum)
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
