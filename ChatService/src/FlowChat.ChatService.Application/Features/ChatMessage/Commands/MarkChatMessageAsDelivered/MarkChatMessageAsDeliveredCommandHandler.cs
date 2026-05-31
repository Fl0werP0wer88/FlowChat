using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher)
    : AggregateRootCommandHandlerBase<MarkChatMessageAsDeliveredCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    private ChatMessageAggregate? _message;

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkChatMessageAsDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        _message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (_message is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Chat message not found."));

        if (_message.SequenceNum.HasValue)
            return FlowChatResult<Unit>.Success(Unit.Value);

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(request.ConversationId, cancellationToken);
        _message.MarkAsDelivered(
            maxSequenceNum.GetValueOrDefault() + 1,
            UtcDateTimeOffset.Create(request.DeliveredAtUtc));

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot GetAggregateRoot() => _message ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
