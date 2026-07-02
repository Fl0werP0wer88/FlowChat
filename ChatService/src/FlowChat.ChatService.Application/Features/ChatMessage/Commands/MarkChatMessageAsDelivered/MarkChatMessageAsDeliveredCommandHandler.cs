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
    : AggregateRootUpdateCommandHandlerBaseV2<MarkChatMessageAsDeliveredCommand, Unit, ChatMessageAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    private ChatMessageAggregate? _message;
    private bool _messageChanged;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkChatMessageAsDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        _message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (_message is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Chat message not found."));

        if (_message.DeliveryStatus == DeliveryStatus.Delivered)
        {
            _messageChanged = false;
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        if (!_message.SequenceNum.HasValue)
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Chat message sequence number must be set before marking it as delivered."));

        _message.MarkAsDelivered(UtcDateTimeOffset.Create(request.DeliveredAtUtc));
        _messageChanged = true;

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override ChatMessageAggregate GetAggregateRoot() => _message ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    protected override MutationType GetMutationType(MarkChatMessageAsDeliveredCommand request, ChatMessageAggregate aggregateRoot) =>
        _messageChanged ? MutationType.Updated : MutationType.Unchanged;
}
