using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantAdded;

public sealed class ConversationParticipantsAddedDomainEventHandlerV2
    : BatchAggregateRootAddDomainEventHandlerBase<
        ConversationParticipantsAddedDomainEventV2,
        ConversationParticipant>
{
    private readonly IConversationParticipantWriteRepository _repository;

    public ConversationParticipantsAddedDomainEventHandlerV2(
        IConversationParticipantWriteRepository repository,
        ILocalEventDispatcher dispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantsAddedDomainEventV2,
            ConversationParticipant>> processors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<
            ConversationParticipantsAddedDomainEventV2,
            ConversationParticipant>> deltaProcessors)
        : base(dispatcher, processors, deltaProcessors)
    {
        _repository = repository;
    }

    protected override async Task<FlowChatResult<BatchAggregateDomainEventMutation<ConversationParticipant>>> ExecuteAsync(
        ConversationParticipantsAddedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        var participants = notification.ParticipantUserIds
            .Select(userId => ConversationParticipant.Create(
                Id<ConversationParticipant>.New(),
                notification.ConversationId,
                userId,
                lastReadMessageSequenceNum: notification.InitialReadCursor))
            .ToArray();

        foreach (var participant in participants)
        {
            await _repository.AddAsync(participant, cancellationToken);
        }

        AggregateRoots = participants.ToDictionary(participant => participant.Id);
        return AddBatch(participants.Select(participant => participant.Id).ToArray());
    }
}
