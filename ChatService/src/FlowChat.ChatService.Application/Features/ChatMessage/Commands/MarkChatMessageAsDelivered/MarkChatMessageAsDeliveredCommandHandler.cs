using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<MarkChatMessageAsDeliveredCommand, ChatMessageAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<MarkChatMessageAsDeliveredCommand, Unit, ChatMessageAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<ChatMessageAggregate?>> FetchAggregateRootAsync(
        MarkChatMessageAsDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return FlowChatResult<ChatMessageAggregate?>.Failure(DomainError.NotFound("Chat message not found."));

        return FlowChatResult<ChatMessageAggregate?>.Success(message);
    }

    protected override Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkChatMessageAsDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.DeliveryStatus == DeliveryStatus.Delivered)
        {
            return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
        }

        if (!AggregateRoot.SequenceNum.HasValue)
            return Task.FromResult(FlowChatResult<Unit>.Failure(DomainError.BadRequest("Chat message sequence number must be set before marking it as delivered.")));

        AggregateRoot.MarkAsDelivered(UtcDateTimeOffset.Create(request.DeliveredAtUtc));
        SetUpdated();

        return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
    }
}
