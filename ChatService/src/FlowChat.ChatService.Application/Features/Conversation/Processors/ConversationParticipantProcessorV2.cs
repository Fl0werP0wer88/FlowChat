using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationParticipantProcessorV2<TTrigger>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher publisher)
    : IAggregateBeforeSaveProcessor<TTrigger, ConversationParticipant>
{
    public void CaptureBeforeState(ConversationParticipant aggregate)
    {
    }

    public Task ProcessAsync(
        TTrigger command,
        ConversationParticipant aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        var integrationEvent = mapper.Map<ConversationParticipantChangedIntegrationEventV2>(aggregate) with
        {
            Operation = MapOperation(mutationType)
        };
        return publisher.PublishAsync(
            integrationEvent,
            aggregate.ConversationId.Value.ToString("D"),
            cancellationToken);
    }

    private static OperationType MapOperation(MutationType mutationType) => mutationType switch
    {
        MutationType.Created => OperationType.Created,
        MutationType.Updated => OperationType.Updated,
        MutationType.Deleted => OperationType.Deleted,
        _ => throw new InvalidOperationException("Unchanged participant state cannot be published.")
    };
}
