namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ConversationMessagesPageDto(
    IReadOnlyCollection<ChatMessageDto> Items,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
    bool HasMore);
