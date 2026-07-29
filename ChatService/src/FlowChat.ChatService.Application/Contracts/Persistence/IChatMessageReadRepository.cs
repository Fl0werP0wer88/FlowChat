using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageReadRepository
{
    Task<IReadOnlyCollection<ChatMessageDto>> GetBeforeSequenceAsync(
        Guid conversationId,
        long throughSequenceNum,
        long? beforeSequenceNum,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatMessageDto>> GetAfterSequenceAsync(
        Guid conversationId,
        long afterSequenceNum,
        long throughSequenceNum,
        int limit,
        CancellationToken cancellationToken = default);
}
