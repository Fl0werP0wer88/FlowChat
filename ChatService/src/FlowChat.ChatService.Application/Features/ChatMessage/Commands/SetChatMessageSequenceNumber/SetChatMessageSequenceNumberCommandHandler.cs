using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<SetChatMessageSequenceNumberCommand, ChatMessageAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<SetChatMessageSequenceNumberCommand, long, ChatMessageAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    private ChatMessageAggregate? _message;

    protected override async Task<FlowChatResult<long>> ExecuteAsync(
        SetChatMessageSequenceNumberCommand request,
        CancellationToken cancellationToken)
    {
        _message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (_message is null)
            return FlowChatResult<long>.Failure(DomainError.NotFound("Chat message not found."));

        if (_message.SequenceNum.HasValue)
        {
            return FlowChatResult<long>.Success(_message.SequenceNum.Value);
        }

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(request.ConversationId, cancellationToken);
        var sequenceNum = maxSequenceNum.GetValueOrDefault() + 1;
        _message.SetSequenceNumber(sequenceNum);
        SetUpdated();

        return FlowChatResult<long>.Success(sequenceNum);
    }

    protected override ChatMessageAggregate GetAggregateRoot() => _message ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
