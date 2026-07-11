using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetContactsForUser;

public sealed record GetContactsForUserResponse(
    IReadOnlyCollection<ContactResponse> Contacts) : IServiceOutput;

public sealed record ContactResponse(
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
