using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record OpenDuetConversationRequest(Guid PartnerUserId) : IServiceInput;
