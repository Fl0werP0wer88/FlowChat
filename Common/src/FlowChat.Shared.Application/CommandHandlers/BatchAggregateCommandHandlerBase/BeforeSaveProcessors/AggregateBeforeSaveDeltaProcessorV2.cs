using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
//Review2-2 Zla nazwa Moe BatchAggregateBeforeSaveDeltaProcessorV2
public class AggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate, TValue>
    : IAggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot, IEntity<TAggregate>
    where TValue : notnull
{
    private readonly IMapper _mapper;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IAggregateDeltaProjectionKeyProviderV2<TCommand, TAggregate> _keyProvider;

    public AggregateBeforeSaveDeltaProcessorV2(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IAggregateDeltaProjectionKeyProviderV2<TCommand, TAggregate> keyProvider)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public async Task ProcessAsync(
        TCommand command,
        AggregateDeltaBatch<TAggregate> batch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(batch.Mutations);

        if (batch.Mutations.Count == 0)
        {
            return;
        }

        ValidateBatchOperationType(batch.BatchOperationType);
        ValidateMutations(batch.Mutations);

        var kafkaKey = _keyProvider.GetKafkaKey(command, batch.Mutations);
        if (string.IsNullOrWhiteSpace(kafkaKey))
        {
            throw new InvalidOperationException("The aggregate delta projection Kafka key cannot be null or empty.");
        }

        var delta = batch.Mutations
            .Select(MapDeltaItem)
            .ToArray();
        var integrationEvent = new DeltaProjectionIntegrationEventV2<TValue>
        {
            BatchOperationType = batch.BatchOperationType,
            Delta = delta
        };
        var envelope = new IntegrationEventEnvelope<DeltaProjectionIntegrationEventV2<TValue>>(
            integrationEvent,
            kafkaKey);

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }

    private static void ValidateBatchOperationType(BatchOperationType batchOperationType)
    {
        switch (batchOperationType)
        {
            case BatchOperationType.Created:
            case BatchOperationType.Updated:
            case BatchOperationType.Deleted:
            case BatchOperationType.Mixed:
                return;
            case BatchOperationType.Unspecified:
                throw new InvalidOperationException(
                    "An unspecified batch operation type cannot be published with a non-empty delta.");
            default:
                throw new ArgumentOutOfRangeException(nameof(batchOperationType), batchOperationType, null);
        }
    }

    private DeltaProjectionItemV2<TValue> MapDeltaItem(AggregateDeltaMutation<TAggregate> mutation)
    {
        var aggregate = mutation.Aggregate;
        var value = _mapper.Map<TValue>(aggregate);
        if (value is null)
        {
            throw new InvalidOperationException(
                $"Mapping aggregate '{aggregate.Id.Value}' to '{typeof(TValue).Name}' returned null.");
        }

        return new DeltaProjectionItemV2<TValue>
        {
            SourceAggregateId = aggregate.Id.Value,
            SourceAggregateCreatedAtUtc = aggregate.CreatedAtUtc.Value,
            SourceAggregateModifiedAtUtc = aggregate.LastModifiedAtUtc.Value,
            SourceAggregateDeletedAt = aggregate.DeletedAt?.Value,
            SourceAggregateVersion = aggregate.Version,
            Value = value,
            Operation = MapOperationType(mutation.MutationType)
        };
    }

    private static void ValidateMutations(IReadOnlyList<AggregateDeltaMutation<TAggregate>> mutations)
    {
        foreach (var mutation in mutations)
        {
            if (mutation is null)
            {
                throw new ArgumentException("The aggregate delta mutation list cannot contain null entries.", nameof(mutations));
            }

            if (mutation.Aggregate is null)
            {
                throw new ArgumentException("An aggregate delta mutation cannot contain a null aggregate.", nameof(mutations));
            }

            _ = MapOperationType(mutation.MutationType);
        }
    }

    private static OperationType MapOperationType(MutationType mutationType)
    {
        return mutationType switch
        {
            MutationType.Created => OperationType.Created,
            MutationType.Updated => OperationType.Updated,
            MutationType.Deleted => OperationType.Deleted,
            MutationType.Unchanged => throw new InvalidOperationException(
                "Unchanged mutation type must not be processed as an aggregate delta projection operation."),
            _ => throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null)
        };
    }
}
