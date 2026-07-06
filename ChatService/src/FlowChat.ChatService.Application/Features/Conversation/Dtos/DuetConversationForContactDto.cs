namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record DuetConversationForContactDto(
    Guid PartnerUserId,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum);
