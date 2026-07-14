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
    private TTargetReadModel? _beforeState;

    public PublishProjectionIntegrationEventProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public void CaptureBeforeState(TAggregate aggregate)
    {
        _beforeState = _mapper.Map<TTargetReadModel>(aggregate);
    }

    public async Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        var operationType = MapOperationType(mutationType);
        var readModel = _mapper.Map<TTargetReadModel>(aggregate);

        if (mutationType == MutationType.Updated
            && _beforeState is not null
            && _beforeState.Equals(readModel))
        {
            return;
        }

        var integrationEvent = new ProjectionIntegrationEvent<TTargetReadModel>
        {
            SourceAggregateId = aggregate.Id.Value,
            SourceAggregateCreatedAtUtc = aggregate.CreatedAtUtc.Value,
            SourceAggregateModifiedAtUtc = aggregate.LastModifiedAtUtc.Value,
            SourceAggregateDeletedAt = aggregate.DeletedAt?.Value,
            Value = readModel,
            Operation = operationType,
            SourceAggregateVersion = aggregate.Version
        };
        var envelope = new IntegrationEventEnvelope<ProjectionIntegrationEvent<TTargetReadModel>>(
            integrationEvent,
            aggregate.Id.Value.ToString("D"));

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }

    private static OperationType MapOperationType(MutationType mutationType)
    {
        return mutationType switch
        {
            MutationType.Created => OperationType.Created,
            MutationType.Updated => OperationType.Updated,
            MutationType.Deleted => OperationType.Deleted,
            MutationType.Unchanged => throw new InvalidOperationException(
                "Unchanged mutation type must not be processed as a projection operation."),
            _ => throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null)
        };
    }
}
