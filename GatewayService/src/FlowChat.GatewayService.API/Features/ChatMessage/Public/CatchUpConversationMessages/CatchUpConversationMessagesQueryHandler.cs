using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryHandler(IChatServiceClient chatServiceClient)
    : IQueryHandler<CatchUpConversationMessagesQuery, CatchUpConversationMessagesResult>
{
    public async Task<FlowChatResult<CatchUpConversationMessagesResult>> Handle(
        CatchUpConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var startSequenceNum = request.AfterSequenceNum == long.MaxValue
            ? long.MaxValue
            : request.AfterSequenceNum + 1;
        var range = await chatServiceClient.GetConversationMessagesRangeAscendingAsync(
            request.ConversationId,
            request.RequestingUserId,
            startSequenceNum,
            request.ThroughSequenceNum,
            request.Limit,
            cancellationToken);
        if (!range.IsSuccess)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(range.Error);
        }

        var page = range.Value;
        if (request.AfterSequenceNum > page.CurrentSequenceNum)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest(
                    "AfterSequenceNum cannot exceed the current conversation sequence."));
        }

        if (request.ThroughSequenceNum > page.CurrentSequenceNum)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest(
                    "ThroughSequenceNum cannot exceed the current conversation sequence."));
        }

        var items = request.AfterSequenceNum == page.CurrentSequenceNum
            ? []
            : page.Items;
        var hasMore = items.Count > 0 && page.HasMore;

        return FlowChatResult<CatchUpConversationMessagesResult>.Success(
            new CatchUpConversationMessagesResult(
                items,
                hasMore ? items.Last().SequenceNum : null,
                page.CurrentSequenceNum,
                page.EndSequenceNum,
                hasMore));
    }
}
