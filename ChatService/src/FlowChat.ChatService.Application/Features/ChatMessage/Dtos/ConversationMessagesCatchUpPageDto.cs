namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ConversationMessagesCatchUpPageDto(
    IReadOnlyCollection<ChatMessageDto> Items,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long ThroughSequenceNum,
    bool HasMore);
