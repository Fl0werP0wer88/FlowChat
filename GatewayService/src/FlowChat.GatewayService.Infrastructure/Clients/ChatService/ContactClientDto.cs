namespace FlowChat.GatewayService.Infrastructure.Clients.ChatService;

public sealed record ContactClientDto(
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
