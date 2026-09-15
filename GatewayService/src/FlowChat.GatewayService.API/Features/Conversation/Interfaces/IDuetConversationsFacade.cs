using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;

namespace FlowChat.GatewayService.Api.Features.Conversation.Interfaces;

public interface IDuetConversationsFacade
{
    Task<FlowChatResult<GetDuetConversationsWithPresenceResult>> GetDuetConversationsWithPresenceAsync(
        CancellationToken cancellationToken);
}
