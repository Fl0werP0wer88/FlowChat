using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public class PublishProjectionIntegrationEventProcessor<TCommand, TAggregate, TTargetReadModel>
    : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot, IEntity<TAggregate>
    where TTargetReadModel : notnull
{
    private readonly IMapper _mapper;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;

    public PublishProjectionIntegrationEventProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public async Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        AggregateState aggregateState,
        CancellationToken cancellationToken)
    {
        var operationType = MapOperationType(aggregateState);
        var readModel = _mapper.Map<TTargetReadModel>(aggregate);
        var integrationEvent = new ProjectionIntegrationEvent<TTargetReadModel>
        {
            Value = readModel,
            Operation = operationType,
            Version = aggregate.Version
        };
        var envelope = new IntegrationEventEnvelope<ProjectionIntegrationEvent<TTargetReadModel>>(
            integrationEvent,
            aggregate.Id.Value.ToString("D"));

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }

    private static OperationType MapOperationType(AggregateState aggregateState)
    {
        return aggregateState switch
        {
            AggregateState.Created => OperationType.Created,
            AggregateState.Updated => OperationType.Updated,
            AggregateState.Deleted => OperationType.Deleted,
            AggregateState.Unchanged => throw new InvalidOperationException(
                "Unchanged aggregate state must not be processed as a projection operation."),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregateState), aggregateState, null)
        };
    }
}
