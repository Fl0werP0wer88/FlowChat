using FlowChat.GatewayService.Api.Services;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed record CatchUpConversationMessagesResult(
    IReadOnlyCollection<ChatMessageClientDto> Items,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long ThroughSequenceNum,
    bool HasMore);
