using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;

public sealed record GetConversationMessagesRangeDescendingQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    long? StartSequenceNum,
    long? EndSequenceNum,
    int Limit) : IQuery<ConversationMessagesRangeDescendingPageDto>;
