using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Eventing.DomainEvents.ConversationParticipantRemoved;

public sealed class ConversationParticipantRemovedDomainEventHandlerV2
    : AggregateRootDeleteDomainEventHandlerBase<
        ConversationParticipantRemovedDomainEventV2,
        ConversationParticipant>
{
    private readonly IConversationParticipantWriteRepository _repository;

    public ConversationParticipantRemovedDomainEventHandlerV2(
        IConversationParticipantWriteRepository repository,
        ILocalEventDispatcher dispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<
            ConversationParticipantRemovedDomainEventV2,
            ConversationParticipant>> processors)
        : base(dispatcher, processors)
    {
        _repository = repository;
    }

    protected override async Task<FlowChatResult<ConversationParticipant?>> FetchAggregateRootAsync(
        ConversationParticipantRemovedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        var participant = await _repository.GetActiveAsync(
            notification.ConversationId,
            notification.UserId,
            cancellationToken);
        return participant is null
            ? FlowChatResult<ConversationParticipant?>.Failure(
                DomainError.NotFound("Conversation participant not found."))
            : FlowChatResult<ConversationParticipant?>.Success(participant);
    }

    protected override Task<FlowChatResult<MutationType>> ExecuteAsync(
        ConversationParticipantRemovedDomainEventV2 notification,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Deleted());
    }
}
