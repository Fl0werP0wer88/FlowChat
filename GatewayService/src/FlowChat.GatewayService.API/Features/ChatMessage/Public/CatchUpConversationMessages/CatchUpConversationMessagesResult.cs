using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed record CatchUpConversationMessagesResult(
    IReadOnlyCollection<ChatMessageClientDto> Items,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long ThroughSequenceNum,
    bool HasMore);
