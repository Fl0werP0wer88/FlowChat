using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationMetadataProcessorV2<TTrigger>(
    IMapper mapper,
    IOutboxIntegrationEventPublisher publisher)
    : IAggregateBeforeSaveProcessor<TTrigger, ConversationV2>
{
    public void CaptureBeforeState(ConversationV2 aggregate)
    {
    }

    public Task ProcessAsync(
        TTrigger command,
        ConversationV2 aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        var integrationEvent = mapper.Map<ConversationChangedIntegrationEventV2>(aggregate) with
        {
            Operation = MapOperation(mutationType)
        };
        return publisher.PublishAsync(
            integrationEvent,
            aggregate.Id.Value.ToString("D"),
            cancellationToken);
    }

    private static OperationType MapOperation(MutationType mutationType) => mutationType switch
    {
        MutationType.Created => OperationType.Created,
        MutationType.Updated => OperationType.Updated,
        MutationType.Deleted => OperationType.Deleted,
        _ => throw new InvalidOperationException("Unchanged conversation metadata cannot be published.")
    };
}
