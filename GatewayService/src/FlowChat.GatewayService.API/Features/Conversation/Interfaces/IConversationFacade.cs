using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

namespace FlowChat.GatewayService.Api.Features.Conversation.Interfaces;

public interface IConversationFacade
{
    Task<FlowChatResult<OpenDuetConversationResult>> OpenDuetAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        Guid? knownConversationId,
        CancellationToken cancellationToken);

    Task<FlowChatResult<OpenGroupConversationResult>> OpenGroupAsync(
        Guid requestingUserId,
        Guid conversationId,
        CancellationToken cancellationToken);
}
