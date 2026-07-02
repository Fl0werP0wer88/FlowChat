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
    : AggregateRootUpdateCommandHandlerBaseV2<SetChatMessageSequenceNumberCommand, long, ChatMessageAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    private ChatMessageAggregate? _message;
    private bool _messageChanged;

    protected override async Task<FlowChatResult<long>> ExecuteAsync(
        SetChatMessageSequenceNumberCommand request,
        CancellationToken cancellationToken)
    {
        _message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (_message is null)
            return FlowChatResult<long>.Failure(DomainError.NotFound("Chat message not found."));

        if (_message.SequenceNum.HasValue)
        {
            _messageChanged = false;
            return FlowChatResult<long>.Success(_message.SequenceNum.Value);
        }

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(request.ConversationId, cancellationToken);
        var sequenceNum = maxSequenceNum.GetValueOrDefault() + 1;
        _message.SetSequenceNumber(sequenceNum);
        _messageChanged = true;

        return FlowChatResult<long>.Success(sequenceNum);
    }

    protected override ChatMessageAggregate GetAggregateRoot() => _message ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    protected override MutationType GetMutationType(SetChatMessageSequenceNumberCommand request, ChatMessageAggregate aggregateRoot) =>
        _messageChanged ? MutationType.Updated : MutationType.Unchanged;
}
