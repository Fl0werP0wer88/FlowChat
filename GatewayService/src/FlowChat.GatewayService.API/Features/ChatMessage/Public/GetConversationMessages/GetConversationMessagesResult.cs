using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed record GetConversationMessagesResult(
    IReadOnlyCollection<ChatMessageClientDto> Items,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);
