using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors.Interfaces;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public class PublishDeltaProjectionIntegrationEventProcessor<TCommand, TAggregate, TDomainEntity, TValue>
    : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot, IEntity<TAggregate>
    where TDomainEntity : class, IEntity<TDomainEntity>
    where TValue : notnull
{
    private readonly IMapper _mapper;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IDeltaProjectionRevisionProvider<TAggregate, TValue> _revisionProvider;
    private IReadOnlyDictionary<Id<TDomainEntity>, TValue>? _beforeState;

    public PublishDeltaProjectionIntegrationEventProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IDeltaProjectionRevisionProvider<TAggregate, TValue> revisionProvider)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _revisionProvider = revisionProvider ?? throw new ArgumentNullException(nameof(revisionProvider));
    }

    public void CaptureBeforeState(TAggregate aggregate)
    {
        _beforeState = MapValuesById(aggregate);
    }

    public async Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        MutationType mutationType,
        CancellationToken cancellationToken)
    {
        if (mutationType == MutationType.Unchanged)
        {
            throw new InvalidOperationException(
                "Unchanged mutation type must not be processed as a delta projection operation.");
        }

        var afterState = MapValuesById(aggregate);

        switch (mutationType)
        {
            case MutationType.Created:
                await PublishAsync(aggregate, afterState.Values, DeltaOperationType.Added, cancellationToken);
                break;
            case MutationType.Updated:
                if (_beforeState is null)
                {
                    throw new InvalidOperationException(
                        "A pre-mutation snapshot is required to process an updated delta projection.");
                }

                var added = afterState
                    .Where(pair => !_beforeState.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .ToArray();
                var removed = _beforeState
                    .Where(pair => !afterState.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .ToArray();

                await PublishAsync(aggregate, added, DeltaOperationType.Added, cancellationToken);
                await PublishAsync(aggregate, removed, DeltaOperationType.Removed, cancellationToken);
                break;
            case MutationType.Deleted:
                await PublishAsync(
                    aggregate,
                    (_beforeState ?? afterState).Values,
                    DeltaOperationType.Removed,
                    cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null);
        }
    }

    private IReadOnlyDictionary<Id<TDomainEntity>, TValue> MapValuesById(TAggregate aggregate)
    {
        return _mapper
            .Map<IEnumerable<TDomainEntity>>(aggregate)
            .ToDictionary(entity => entity.Id, entity => _mapper.Map<TValue>(entity));
    }

    private async Task PublishAsync(
        TAggregate aggregate,
        IEnumerable<TValue> values,
        DeltaOperationType operation,
        CancellationToken cancellationToken)
    {
        var materializedValues = values.ToArray();

        if (materializedValues.Length == 0)
        {
            return;
        }

        var integrationEvent = new DeltaProjectionIntegrationEvent<TValue>
        {
            SourceAggregateId = aggregate.Id.Value,
            SourceAggregateCreatedAtUtc = aggregate.CreatedAtUtc.Value,
            SourceAggregateModifiedAtUtc = aggregate.LastModifiedAtUtc.Value,
            SourceAggregateDeletedAt = aggregate.DeletedAt?.Value,
            Value = materializedValues,
            Operation = operation,
            SourceAggregateVersion = aggregate.Version,
            ProjectionRevision = _revisionProvider.GetRevision(aggregate)
        };
        var envelope = new IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TValue>>(
            integrationEvent,
            aggregate.Id.Value.ToString("D"));

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }
}
