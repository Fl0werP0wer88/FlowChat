using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandHandlerV2(
    IChatMessageV2WriteRepository messageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher dispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<MarkChatMessageAsDeliveredCommandV2, ChatMessageV2>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<
        MarkChatMessageAsDeliveredCommandV2,
        Unit,
        ChatMessageV2>(dispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ChatMessageV2?>> FetchAggregateRootAsync(
        MarkChatMessageAsDeliveredCommandV2 request,
        CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(
            Id<ChatMessageV2>.FromGuid(request.MessageId),
            cancellationToken);
        return message is null
            ? FlowChatResult<ChatMessageV2?>.Failure(DomainError.NotFound("Chat message not found."))
            : FlowChatResult<ChatMessageV2?>.Success(message);
    }

    protected override Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(
        MarkChatMessageAsDeliveredCommandV2 request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.ConversationId.Value != request.ConversationId)
        {
            return Task.FromResult(
                Failure(DomainError.NotFound("Chat message not found.")));
        }

        if (AggregateRoot.DeliveryStatus == DeliveryStatus.Delivered)
            return Task.FromResult(Unchanged(Unit.Value));

        AggregateRoot.MarkAsDelivered(UtcDateTimeOffset.Create(request.DeliveredAtUtc));
        return Task.FromResult(Updated(Unit.Value));
    }
}
