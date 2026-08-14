using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;

public sealed record OpenDuetConversationRequest(
    Guid PartnerUserId,
    Guid? KnownConversationId) : IServiceInput;
