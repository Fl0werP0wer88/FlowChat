namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ConversationMessageHistoryPageDto(
    IReadOnlyCollection<ChatMessageDto> Items,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);
