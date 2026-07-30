using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

public sealed record OpenGroupConversationResult(
    Guid ConversationId,
    string Name,
    IReadOnlyCollection<ConversationParticipantClientDto> Participants,
    IReadOnlyCollection<ChatMessageClientDto> Messages,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);
