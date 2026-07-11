namespace FlowChat.GatewayService.Api.Services;

public sealed record ContactClientDto(
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
