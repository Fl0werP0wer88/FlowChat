using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Api.Models;

public sealed record ContactWithConversationDto(
    Guid ContactUserId,
    string? DisplayName,
    string? AvatarUrl,
    bool IsBlocked,
    bool IsBlockedByPartner,
    bool IsMuted,
    bool IsHidden,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum,
    long UnreadCount,
    PresenceStatus Status,
    DateTimeOffset PresenceChangedAtUtc) : IServiceOutput;
