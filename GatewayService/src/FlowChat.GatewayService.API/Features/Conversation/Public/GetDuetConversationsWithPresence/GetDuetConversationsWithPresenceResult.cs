using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;

public sealed record GetDuetConversationsWithPresenceResult(
    IReadOnlyCollection<DuetConversationWithPresenceResult> Conversations);

public sealed record DuetConversationWithPresenceResult(
    Guid PartnerUserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Email,
    bool IsBlocked,
    bool IsBlockedByPartner,
    bool IsMuted,
    bool IsHidden,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum,
    long UnreadCount,
    PresenceStatus Status,
    DateTimeOffset PresenceChangedAtUtc);
