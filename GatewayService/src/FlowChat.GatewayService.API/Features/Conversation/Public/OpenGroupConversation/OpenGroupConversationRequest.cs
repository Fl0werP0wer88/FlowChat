using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

public sealed record OpenGroupConversationRequest(Guid ConversationId) : IServiceInput;
