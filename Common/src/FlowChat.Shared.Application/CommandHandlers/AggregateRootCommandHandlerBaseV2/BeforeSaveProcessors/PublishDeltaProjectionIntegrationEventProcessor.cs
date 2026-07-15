using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public abstract class PublishDeltaProjectionIntegrationEventProcessor<TCommand, TAggregate, TValue, TKey>
    : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot, IEntity<TAggregate>
    where TValue : notnull
    where TKey : notnull
{
    private readonly IMapper _mapper;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private IReadOnlyCollection<TValue>? _beforeState;

    protected PublishDeltaProjectionIntegrationEventProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public void CaptureBeforeState(TAggregate aggregate)
    {
        _beforeState = MapValues(aggregate);
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

        var afterState = MapValues(aggregate);

        switch (mutationType)
        {
            case MutationType.Created:
                await PublishAsync(aggregate, afterState, DeltaOperationType.Added, cancellationToken);
                break;
            case MutationType.Updated:
                if (_beforeState is null)
                {
                    throw new InvalidOperationException(
                        "A pre-mutation snapshot is required to process an updated delta projection.");
                }

                var beforeByKey = IndexByKey(_beforeState);
                var afterByKey = IndexByKey(afterState);
                var added = afterByKey
                    .Where(pair => !beforeByKey.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .ToArray();
                var removed = beforeByKey
                    .Where(pair => !afterByKey.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .ToArray();

                await PublishAsync(aggregate, added, DeltaOperationType.Added, cancellationToken);
                await PublishAsync(aggregate, removed, DeltaOperationType.Removed, cancellationToken);
                break;
            case MutationType.Deleted:
                await PublishAsync(
                    aggregate,
                    _beforeState ?? afterState,
                    DeltaOperationType.Removed,
                    cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null);
        }
    }

    protected abstract TKey GetKey(TValue value);

    private IReadOnlyCollection<TValue> MapValues(TAggregate aggregate)
    {
        return _mapper.Map<IEnumerable<TValue>>(aggregate).ToArray();
    }

    private Dictionary<TKey, TValue> IndexByKey(IEnumerable<TValue> values)
    {
        return values.ToDictionary(GetKey);
    }

    private async Task PublishAsync(
        TAggregate aggregate,
        IReadOnlyCollection<TValue> values,
        DeltaOperationType operation,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
        {
            return;
        }

        var integrationEvent = new DeltaProjectionIntegrationEvent<TValue>
        {
            SourceAggregateId = aggregate.Id.Value,
            SourceAggregateCreatedAtUtc = aggregate.CreatedAtUtc.Value,
            SourceAggregateModifiedAtUtc = aggregate.LastModifiedAtUtc.Value,
            SourceAggregateDeletedAt = aggregate.DeletedAt?.Value,
            Value = values,
            Operation = operation,
            SourceAggregateVersion = aggregate.Version
        };
        var envelope = new IntegrationEventEnvelope<DeltaProjectionIntegrationEvent<TValue>>(
            integrationEvent,
            aggregate.Id.Value.ToString("D"));

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }
}
