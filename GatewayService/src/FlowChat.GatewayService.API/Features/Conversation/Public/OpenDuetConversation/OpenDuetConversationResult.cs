using FlowChat.GatewayService.Infrastructure.Clients.ChatService;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;

public sealed record OpenDuetConversationResult(
    Guid ConversationId,
    IReadOnlyCollection<ConversationParticipantClientDto> Participants,
    IReadOnlyCollection<ChatMessageClientDto> Messages,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore);
