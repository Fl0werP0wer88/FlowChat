using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public abstract class BatchAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    : Notifications.DomainEventHandlerBase<TNotification>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IReadOnlyList<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> _beforeSaveProcessors;
    private readonly IReadOnlyList<IAggregateBeforeSaveDeltaProcessorV2<TNotification, TAggregate>> _beforeSaveDeltaProcessors;

    protected BatchAggregateRootDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TNotification, TAggregate>> beforeSaveDeltaProcessors)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors?.ToArray()
            ?? throw new ArgumentNullException(nameof(beforeSaveProcessors));
        _beforeSaveDeltaProcessors = beforeSaveDeltaProcessors?.ToArray()
            ?? throw new ArgumentNullException(nameof(beforeSaveDeltaProcessors));
    }

    protected IReadOnlyDictionary<Id<TAggregate>, TAggregate> AggregateRoots { get; set; }
        = new Dictionary<Id<TAggregate>, TAggregate>();

    protected override async Task HandleNotificationAsync(
        TNotification notification,
        CancellationToken cancellationToken)
    {
        var executionResult = await ExecuteAsync(notification, cancellationToken);

        if (executionResult.IsFailure)
        {
            throw new ResultException(FlowChatResult.Failure(executionResult.Error));
        }

        var operationResult = executionResult.Value
            ?? throw new InvalidOperationException("The batch mutation result cannot be null.");
        var aggregateRoots = ValidateAndGetAggregateRoots(operationResult.Mutations);
        BatchOperationTypeValidator.Validate(
            operationResult.BatchOperationType,
            operationResult.Mutations.Any(mutation => mutation.MutationType != MutationType.Unchanged));

        foreach (var mutation in operationResult.Mutations)
        {
            if (mutation.MutationType == MutationType.Unchanged)
            {
                continue;
            }

            var aggregateRoot = aggregateRoots[mutation.Id];
            aggregateRoot.IncrementVersion();

            var domainEvents = aggregateRoot.PopDomainEvents();
            DomainEventBase.StampVersions(domainEvents, aggregateRoot.Version);
            var localEvents = domainEvents.Cast<ILocalEvent>();

            await _localEventsDispatcher.DispatchAsync(localEvents, cancellationToken);
            ApplyAuditInfo(aggregateRoot, mutation.MutationType);

            foreach (var processor in _beforeSaveProcessors)
            {
                await processor.ProcessAsync(notification, aggregateRoot, mutation.MutationType, cancellationToken);
            }
        }

        if (_beforeSaveDeltaProcessors.Count == 0)
        {
            return;
        }

        var deltaMutations = operationResult.Mutations
            .Where(mutation => mutation.MutationType != MutationType.Unchanged)
            .Select(mutation => new AggregateDeltaMutation<TAggregate>(
                aggregateRoots[mutation.Id],
                mutation.MutationType))
            .ToArray();

        if (deltaMutations.Length == 0)
        {
            return;
        }

        var deltaBatch = new AggregateDeltaBatch<TAggregate>(
            operationResult.BatchOperationType,
            deltaMutations);

        foreach (var processor in _beforeSaveDeltaProcessors)
        {
            await processor.ProcessAsync(notification, deltaBatch, cancellationToken);
        }
    }

    protected static FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>> Unchanged() =>
        Mutations([], BatchOperationType.Unspecified);

    protected static FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>> Failure(
        IDomainError error) =>
        FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>>.Failure(error);

    protected static FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>> Mutations(
        IReadOnlyList<AggregateMutationDescriptor<TAggregate>> mutations,
        BatchOperationType batchOperationType)
    {
        ArgumentNullException.ThrowIfNull(mutations);

        return FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>>.Success(
            new BatchAggregateDomainEventMutation<TAggregate>(batchOperationType, mutations));
    }

    protected static FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>> Mutations(
        IReadOnlyList<Id<TAggregate>> aggregateIds,
        MutationType mutationType,
        BatchOperationType batchOperationType)
    {
        ArgumentNullException.ThrowIfNull(aggregateIds);

        var mutations = aggregateIds
            .Select(aggregateId => new AggregateMutationDescriptor<TAggregate>(aggregateId, mutationType))
            .ToArray();

        return Mutations(mutations, batchOperationType);
    }

    protected abstract Task<FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>>> ExecuteAsync(
        TNotification notification,
        CancellationToken cancellationToken);

    private IReadOnlyDictionary<Id<TAggregate>, TAggregate> ValidateAndGetAggregateRoots(
        IReadOnlyList<AggregateMutationDescriptor<TAggregate>> mutations)
    {
        if (mutations is null)
        {
            throw new InvalidOperationException("The batch mutation list cannot be null.");
        }

        var aggregateRoots = AggregateRoots
            ?? throw new InvalidOperationException("The aggregate roots dictionary cannot be null.");
        var aggregateIds = new HashSet<Id<TAggregate>>();

        foreach (var mutation in mutations)
        {
            if (mutation is null)
            {
                throw new InvalidOperationException("The batch mutation list cannot contain null entries.");
            }

            if (mutation.Id is null)
            {
                throw new InvalidOperationException("A batch mutation aggregate id cannot be null.");
            }

            if (!Enum.IsDefined(mutation.MutationType))
            {
                throw new InvalidOperationException($"Unsupported mutation type: {mutation.MutationType}.");
            }

            if (!aggregateIds.Add(mutation.Id))
            {
                throw new InvalidOperationException(
                    $"The batch mutation list contains duplicate aggregate id '{mutation.Id.Value}'.");
            }

            if (!aggregateRoots.ContainsKey(mutation.Id))
            {
                throw new InvalidOperationException(
                    $"Aggregate root with id '{mutation.Id.Value}' is not available.");
            }
        }

        return aggregateRoots;
    }

    private static void ApplyAuditInfo(TAggregate aggregateRoot, MutationType mutationType)
    {
        const string SystemActor = "system";

        switch (mutationType)
        {
            case MutationType.Created:
                aggregateRoot.SetCreated(SystemActor);
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case MutationType.Updated:
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case MutationType.Deleted:
                aggregateRoot.SetUpdated(SystemActor);
                aggregateRoot.Delete(UtcDateTimeOffset.UtcNow);
                break;
            case MutationType.Unchanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutationType), mutationType, null);
        }
    }
}
