using FlowChat.Shared.Application;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed record CatchUpConversationMessagesQuery(
    Guid ConversationId,
    Guid RequestingUserId,
    int Limit,
    long AfterSequenceNum,
    long? ThroughSequenceNum) : IQuery<CatchUpConversationMessagesResult>;
