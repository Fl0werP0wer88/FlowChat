using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantAdded;

public sealed class ConversationParticipantAddedDomainEventHandlerV2
    : AggregateRootInsertDomainEventHandlerBase<
        ConversationParticipantAddedDomainEventV2,
        ConversationParticipant>
{
    private readonly IConversationParticipantWriteRepository _repository;
    private ConversationParticipant? _participant;

    public ConversationParticipantAddedDomainEventHandlerV2(
        IConversationParticipantWriteRepository repository,
        ILocalEventDispatcher dispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantAddedDomainEventV2,
            ConversationParticipant>> processors)
        : base(dispatcher, processors)
    {
        _repository = repository;
    }

    protected override async Task<FlowChatResult<MutationType>> ExecuteAsync(
        ConversationParticipantAddedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        _participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            notification.ConversationId,
            notification.UserId,
            lastReadMessageSequenceNum: notification.InitialReadCursor);
        await _repository.AddAsync(_participant, cancellationToken);
        return Created();
    }

    protected override ConversationParticipant GetAggregateRoot() =>
        _participant ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
