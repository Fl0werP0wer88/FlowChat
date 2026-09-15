namespace FlowChat.ChatService.Application.Features.Conversation.Dtos;

public sealed record DuetConversationListItemDto(
    Guid PartnerUserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Email,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum,
    bool IsBlocked,
    bool IsBlockedByPartner,
    bool IsMuted,
    bool IsHidden);
