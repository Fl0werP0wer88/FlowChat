using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.Shared.Domain;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Services;

public sealed class ConversationMessagesFacade(IChatServiceClient chatServiceClient)
    : IConversationMessagesFacade
{
    public async Task<FlowChatResult<GetConversationMessagesResult>> GetHistoryAsync(
        Guid conversationId,
        Guid requestingUserId,
        int limit,
        long? beforeSequenceNum,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCommon(conversationId, requestingUserId, limit);
        if (validationError is not null)
        {
            return FlowChatResult<GetConversationMessagesResult>.Failure(validationError);
        }

        if (beforeSequenceNum < 1)
        {
            return FlowChatResult<GetConversationMessagesResult>.Failure(
                DomainError.BadRequest("BeforeSequenceNum must be greater than or equal to 1."));
        }

        var endSequenceNum = beforeSequenceNum - 1;
        var range = await chatServiceClient.GetConversationMessagesRangeDescendingAsync(
            conversationId,
            requestingUserId,
            1,
            endSequenceNum,
            limit,
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

    public async Task<FlowChatResult<CatchUpConversationMessagesResult>> CatchUpAsync(
        Guid conversationId,
        Guid requestingUserId,
        int limit,
        long afterSequenceNum,
        long? throughSequenceNum,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCommon(conversationId, requestingUserId, limit);
        if (validationError is not null)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(validationError);
        }

        if (afterSequenceNum < 0)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest("AfterSequenceNum must be greater than or equal to 0."));
        }

        if (throughSequenceNum < afterSequenceNum)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest(
                    "ThroughSequenceNum must be greater than or equal to AfterSequenceNum."));
        }

        var startSequenceNum = afterSequenceNum == long.MaxValue
            ? long.MaxValue
            : afterSequenceNum + 1;
        var range = await chatServiceClient.GetConversationMessagesRangeAscendingAsync(
            conversationId,
            requestingUserId,
            startSequenceNum,
            throughSequenceNum,
            limit,
            cancellationToken);
        if (!range.IsSuccess)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(range.Error);
        }

        var page = range.Value;
        if (afterSequenceNum > page.CurrentSequenceNum)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest(
                    "AfterSequenceNum cannot exceed the current conversation sequence."));
        }

        if (throughSequenceNum > page.CurrentSequenceNum)
        {
            return FlowChatResult<CatchUpConversationMessagesResult>.Failure(
                DomainError.BadRequest(
                    "ThroughSequenceNum cannot exceed the current conversation sequence."));
        }

        var items = afterSequenceNum == page.CurrentSequenceNum
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

    private static DomainError? ValidateCommon(
        Guid conversationId,
        Guid requestingUserId,
        int limit)
    {
        if (conversationId == Guid.Empty)
        {
            return DomainError.BadRequest("ConversationId is required.");
        }

        if (requestingUserId == Guid.Empty)
        {
            return DomainError.BadRequest("RequestingUserId is required.");
        }

        return limit is < 1 or > 100
            ? DomainError.BadRequest("Limit must be between 1 and 100.")
            : null;
    }
}
