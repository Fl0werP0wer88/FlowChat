using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversations;

public sealed record GetGroupConversationsResponse(
    IReadOnlyCollection<GroupConversationSummaryResponse> GroupConversations) : IServiceOutput;

public sealed record GroupConversationSummaryResponse(
    Guid ConversationId,
    string Name,
    int ParticipantCount,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum);
