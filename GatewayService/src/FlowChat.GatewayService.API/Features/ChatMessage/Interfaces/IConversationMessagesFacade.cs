using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;

public interface IConversationMessagesFacade
{
    Task<FlowChatResult<GetConversationMessagesResult>> GetHistoryAsync(
        Guid conversationId,
        Guid requestingUserId,
        int limit,
        long? beforeSequenceNum,
        CancellationToken cancellationToken);

    Task<FlowChatResult<CatchUpConversationMessagesResult>> CatchUpAsync(
        Guid conversationId,
        Guid requestingUserId,
        int limit,
        long afterSequenceNum,
        long? throughSequenceNum,
        CancellationToken cancellationToken);
}
