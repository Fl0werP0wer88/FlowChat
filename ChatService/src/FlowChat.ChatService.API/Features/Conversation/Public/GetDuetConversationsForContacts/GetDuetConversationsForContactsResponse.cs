using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationsForContacts;

public sealed record GetDuetConversationsForContactsResponse(
    IReadOnlyCollection<DuetConversationForContactResponse> Conversations) : IServiceOutput;

public sealed record DuetConversationForContactResponse(
    Guid PartnerUserId,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum);
