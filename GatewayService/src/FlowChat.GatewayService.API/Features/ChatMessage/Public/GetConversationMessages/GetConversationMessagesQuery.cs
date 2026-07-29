using FlowChat.Shared.Application;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    int Limit,
    long? BeforeSequenceNum) : IQuery<GetConversationMessagesResult>;
