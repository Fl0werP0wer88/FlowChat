namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record ContactDto(
    Guid PartnerUserId,
    string? DisplayName,
    string? AvatarUrl,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum,
    bool IsBlocked,
    bool IsBlockedByPartner,
    bool IsMuted,
    bool IsHidden);
