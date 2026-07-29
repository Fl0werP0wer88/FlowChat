using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    int Limit,
    long? BeforeSequenceNum) : IQuery<ConversationMessageHistoryPageDto>;
