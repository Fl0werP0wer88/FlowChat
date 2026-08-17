using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;

public sealed record GetDuetConversationsWithPresenceResponse(
    IReadOnlyCollection<DuetConversationWithPresenceResponse> Conversations) : IServiceOutput;

public sealed record DuetConversationWithPresenceResponse(
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
