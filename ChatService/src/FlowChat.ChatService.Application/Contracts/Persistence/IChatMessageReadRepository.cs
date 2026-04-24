using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IChatMessageReadRepository
{
    Task<ConversationMessagesPageDto> GetPageBeforeAsync(
        Guid conversationId,
        int limit,
        DateTimeOffset? beforeSentAtUtc,
        Guid? beforeMessageId,
        CancellationToken cancellationToken = default);
}
