using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageReadRepository
{
    Task<IReadOnlyCollection<ChatMessageDto>> GetRangeDescendingAsync(
        Guid conversationId,
        long startSequenceNum,
        long endSequenceNum,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatMessageDto>> GetRangeAscendingAsync(
        Guid conversationId,
        long startSequenceNum,
        long endSequenceNum,
        int limit,
        CancellationToken cancellationToken = default);
}
