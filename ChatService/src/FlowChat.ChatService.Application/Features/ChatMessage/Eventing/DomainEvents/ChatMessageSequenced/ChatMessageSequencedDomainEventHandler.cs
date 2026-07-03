using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSequenced;

public sealed class ChatMessageSequencedDomainEventHandler(
    IConversationWriteRepository conversationRepository,
    ILocalEventDispatcher localEventsDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessor<ChatMessageSequencedDomainEvent, ConversationAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateDomainEventHandlerBase<ChatMessageSequencedDomainEvent, ConversationAggregate>(
        localEventsDispatcher,
        beforeSaveProcessors)
{
    private ConversationAggregate? _conversation;

    protected override async Task<FlowChatResult> ExecuteAsync(
        ChatMessageSequencedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        _conversation = await conversationRepository.GetByIdAsync(notification.ConversationId, cancellationToken);
        if (_conversation is null)
            return FlowChatResult.Failure(DomainError.NotFound("Conversation not found."));

        var previousSequenceNum = _conversation.LastMsgSequenceNum;
        _conversation.SetSequenceNumber(notification.SequenceNum);

        if (_conversation.LastMsgSequenceNum != previousSequenceNum)
            SetUpdated();

        return FlowChatResult.Success();
    }

    protected override ConversationAggregate GetAggregateRoot()
        => _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
