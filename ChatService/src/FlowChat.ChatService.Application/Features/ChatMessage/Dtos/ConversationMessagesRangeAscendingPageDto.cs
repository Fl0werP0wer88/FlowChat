namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ConversationMessagesRangeAscendingPageDto(
    IReadOnlyCollection<ChatMessageDto> Items,
    long StartSequenceNum,
    long EndSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);
