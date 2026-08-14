using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;

public sealed record GetContactsWithConversationsResponse(
    IReadOnlyCollection<ContactWithConversationResponse> Contacts) : IServiceOutput;

public sealed record ContactWithConversationResponse(
    Guid ContactUserId,
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
