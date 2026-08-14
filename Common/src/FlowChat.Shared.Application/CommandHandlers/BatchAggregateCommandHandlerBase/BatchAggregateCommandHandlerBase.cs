using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

public abstract class BatchAggregateCommandHandlerBase<TCommand, TResponse, TAggregate>
    : TransactionalCommandHandlerBase<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IReadOnlyList<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> _beforeSaveProcessors;
    private readonly IReadOnlyList<IAggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate>> _beforeSaveDeltaProcessors;

    protected BatchAggregateCommandHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> beforeSaveProcessors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate>> beforeSaveDeltaProcessors)
        : base(unitOfWork)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors?.ToArray()
            ?? throw new ArgumentNullException(nameof(beforeSaveProcessors));
        _beforeSaveDeltaProcessors = beforeSaveDeltaProcessors?.ToArray()
            ?? throw new ArgumentNullException(nameof(beforeSaveDeltaProcessors));
    }

    protected IReadOnlyDictionary<Id<TAggregate>, TAggregate> AggregateRoots { get; set; }
        = new Dictionary<Id<TAggregate>, TAggregate>();

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var executionResult = await ExecuteAsync(request, cancellationToken);

        if (executionResult.IsFailure)
        {
            return FlowChatResult<TResponse>.Failure(executionResult.Error);
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
                await processor.ProcessAsync(request, aggregateRoot, mutation.MutationType, cancellationToken);
            }
        }

        if (_beforeSaveDeltaProcessors.Count > 0)
        {
            var deltaMutations = operationResult.Mutations
                .Where(mutation => mutation.MutationType != MutationType.Unchanged)
                .Select(mutation => new AggregateDeltaMutation<TAggregate>(
                    aggregateRoots[mutation.Id],
                    mutation.MutationType))
                .ToArray();

            if (deltaMutations.Length > 0)
            {
                var deltaBatch = new AggregateDeltaBatch<TAggregate>(
                    deltaMutations,
                    operationResult.DeltaProjectionMetadata);

                foreach (var processor in _beforeSaveDeltaProcessors)
                {
                    await processor.ProcessAsync(request, deltaBatch, cancellationToken);
                }
            }
        }

        return FlowChatResult<TResponse>.Success(operationResult.Response);
    }

    protected static FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>> Unchanged(TResponse response) =>
        Mutation(response, [], BatchOperationType.Unspecified);

    protected static FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>> Failure(IDomainError error) =>
        FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>>.Failure(error);

    protected static FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>> Mutation(
        TResponse response,
        IReadOnlyList<AggregateMutationDescriptor<TAggregate>> mutations,
        BatchOperationType batchOperationType,
        DeltaProjectionMetadataV2? deltaProjectionMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(mutations);

        return FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>>.Success(
            new BatchAggregateMutation<TResponse, TAggregate>(
                response,
                batchOperationType,
                mutations,
                deltaProjectionMetadata));
    }

    protected static FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>> Mutation(
        TResponse response,
        IReadOnlyList<Id<TAggregate>> aggregateIds,
        MutationType mutationType,
        BatchOperationType batchOperationType,
        DeltaProjectionMetadataV2? deltaProjectionMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(aggregateIds);

        var mutations = aggregateIds
            .Select(aggregateId => new AggregateMutationDescriptor<TAggregate>(aggregateId, mutationType))
            .ToArray();

        return Mutation(response, mutations, batchOperationType, deltaProjectionMetadata);
    }

    protected abstract Task<FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>>> ExecuteAsync(
        TCommand request,
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
