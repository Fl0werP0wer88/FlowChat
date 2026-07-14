namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record GroupConversationSummaryDto(
    Guid ConversationId,
    string Name,
    int ParticipantCount,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum);
