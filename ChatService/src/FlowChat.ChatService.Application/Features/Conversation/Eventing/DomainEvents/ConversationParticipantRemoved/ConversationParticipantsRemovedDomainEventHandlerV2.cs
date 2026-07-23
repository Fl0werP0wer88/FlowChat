using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantRemoved;

public sealed class ConversationParticipantsRemovedDomainEventHandlerV2
    : BatchAggregateRootRemoveDomainEventHandlerBase<
        ConversationParticipantsRemovedDomainEventV2,
        ConversationParticipant>
{
    private readonly IConversationParticipantWriteRepository _repository;

    public ConversationParticipantsRemovedDomainEventHandlerV2(
        IConversationParticipantWriteRepository repository,
        ILocalEventDispatcher dispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>> processors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<
            ConversationParticipantsRemovedDomainEventV2,
            ConversationParticipant>> deltaProcessors)
        : base(dispatcher, processors, deltaProcessors)
    {
        _repository = repository;
    }

    protected override async Task<FlowChatResult<BatchAggregateDomainEventMutation<ConversationParticipant>>> ExecuteAsync(
        ConversationParticipantsRemovedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        var fetchedParticipants = await _repository.GetActiveByUserIdsAsync(
            notification.ConversationId,
            notification.ParticipantUserIds,
            cancellationToken);
        var participantsByUserId = fetchedParticipants.ToDictionary(participant => participant.UserId);

        if (notification.ParticipantUserIds.Any(userId => !participantsByUserId.ContainsKey(userId)))
        {
            return Failure(DomainError.NotFound("Conversation participant not found."));
        }

        var orderedParticipants = notification.ParticipantUserIds
            .Select(userId => participantsByUserId[userId])
            .ToArray();

        AggregateRoots = orderedParticipants.ToDictionary(participant => participant.Id);
        return Success(
            orderedParticipants.Select(participant => participant.Id).ToArray(),
            new DeltaProjectionMetadataV2(
                notification.ConversationId.Value,
                notification.Version));
    }
}
