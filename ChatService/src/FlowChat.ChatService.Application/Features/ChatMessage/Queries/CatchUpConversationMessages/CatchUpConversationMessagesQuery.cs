using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;

public sealed record CatchUpConversationMessagesQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    int Limit,
    long AfterSequenceNum,
    long? ThroughSequenceNum) : IQuery<ConversationMessagesCatchUpPageDto>;
