using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.Application;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler(IChatServiceClient chatServiceClient)
    : IQueryHandler<GetConversationMessagesQuery, GetConversationMessagesResult>
{
    public async Task<FlowChatResult<GetConversationMessagesResult>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        long? endSequenceNum = request.BeforeSequenceNum.HasValue
            ? request.BeforeSequenceNum.Value - 1
            : null;
        var range = await chatServiceClient.GetConversationMessagesRangeDescendingAsync(
            request.ConversationId,
            request.RequestingUserId,
            1,
            endSequenceNum,
            request.Limit,
            cancellationToken);
        if (!range.IsSuccess)
        {
            return FlowChatResult<GetConversationMessagesResult>.Failure(range.Error);
        }

        var page = range.Value;
        return FlowChatResult<GetConversationMessagesResult>.Success(
            new GetConversationMessagesResult(
                page.Items,
                page.HasMore ? page.Items.Last().SequenceNum : null,
                page.CurrentSequenceNum,
                page.HasMore));
    }
}
