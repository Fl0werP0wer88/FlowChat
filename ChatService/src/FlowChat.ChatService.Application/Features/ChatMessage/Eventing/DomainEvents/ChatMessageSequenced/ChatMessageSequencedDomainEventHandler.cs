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
    protected override async Task<FlowChatResult<ConversationAggregate?>> FetchAggregateRootAsync(
        ChatMessageSequencedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(notification.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<ConversationAggregate?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<ConversationAggregate?>.Success(conversation);
    }

    protected override Task<FlowChatResult> ExecuteAsync(
        ChatMessageSequencedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var previousSequenceNum = AggregateRoot!.LastMsgSequenceNum;
        AggregateRoot.SetSequenceNumber(notification.SequenceNum);

        if (AggregateRoot.LastMsgSequenceNum != previousSequenceNum)
            SetUpdated();

        return Task.FromResult(FlowChatResult.Success());
    }
}
