using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record OpenGroupConversationRequest(
    Guid ConversationId) : IServiceInput;
