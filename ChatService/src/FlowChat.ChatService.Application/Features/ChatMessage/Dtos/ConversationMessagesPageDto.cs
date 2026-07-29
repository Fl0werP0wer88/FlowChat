namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ConversationMessagesPageDto(
    IReadOnlyCollection<ChatMessageDto> Items,
    long? NextBeforeSequenceNum,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long? ThroughSequenceNum,
    bool HasMore);
