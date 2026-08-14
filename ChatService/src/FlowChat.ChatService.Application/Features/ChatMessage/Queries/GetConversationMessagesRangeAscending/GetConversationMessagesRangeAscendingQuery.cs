using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;

public sealed record GetConversationMessagesRangeAscendingQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    long? StartSequenceNum,
    long? EndSequenceNum,
    int Limit) : IQuery<ConversationMessagesRangeAscendingPageDto>;
