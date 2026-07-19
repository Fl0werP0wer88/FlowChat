using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationCreated;

public sealed class ConversationCreatedDomainEventHandlerV2
    : AggregateRootInsertDomainEventHandlerBase<ConversationCreatedDomainEventV2, ConversationMembership>
{
    private readonly IConversationMembershipWriteRepository _repository;
    private ConversationMembership? _membership;

    public ConversationCreatedDomainEventHandlerV2(
        IConversationMembershipWriteRepository repository,
        ILocalEventDispatcher dispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<ConversationCreatedDomainEventV2, ConversationMembership>> processors)
        : base(dispatcher, processors)
    {
        _repository = repository;
    }

    protected override async Task<FlowChatResult> ExecuteAsync(
        ConversationCreatedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        _membership = ConversationMembership.Create(
            Id<ConversationV2>.FromGuid(notification.ConversationId),
            notification.Type,
            notification.ParticipantUserIds);
        await _repository.AddAsync(_membership, cancellationToken);
        SetInserted();
        return FlowChatResult.Success();
    }

    protected override ConversationMembership GetAggregateRoot() =>
        _membership ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
