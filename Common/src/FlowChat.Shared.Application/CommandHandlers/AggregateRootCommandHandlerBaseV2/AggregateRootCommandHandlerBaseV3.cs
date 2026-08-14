using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    : TransactionalCommandHandlerBase<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> _beforeSaveProcessors;

    protected AggregateRootCommandHandlerBaseV3(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> beforeSaveProcessors)
        : base(unitOfWork)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
    }

    protected TAggregate? AggregateRoot { get; set; }

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var executionResult = await ExecuteAsync(request, cancellationToken);

        if (executionResult.IsFailure)
        {
            return FlowChatResult<TResponse>.Failure(executionResult.Error);
        }

        var operationResult = executionResult.Value;
        var aggregateRoot = GetAggregateRoot();

        if (operationResult.MutationType == MutationType.Unchanged)
        {
            return FlowChatResult<TResponse>.Success(operationResult.Response);
        }

        aggregateRoot.IncrementVersion();
        var domainEvents = aggregateRoot.PopDomainEvents();
        DomainEventBase.StampVersions(domainEvents, aggregateRoot.Version);
        var localEvents = domainEvents
            .Cast<ILocalEvent>();

        await DispatchLocalEventsAsync(localEvents, cancellationToken);
        ApplyAuditInfo(aggregateRoot, operationResult.MutationType);

        foreach (var processor in _beforeSaveProcessors)
        {
            await processor.ProcessAsync(request, aggregateRoot, operationResult.MutationType, cancellationToken);
        }

        return FlowChatResult<TResponse>.Success(operationResult.Response);
    }

    protected static FlowChatResult<AggregateMutation<TResponse>> Unchanged(TResponse response) =>
        Mutation(MutationType.Unchanged, response);

    protected static FlowChatResult<AggregateMutation<TResponse>> Failure(IDomainError error) =>
        FlowChatResult<AggregateMutation<TResponse>>.Failure(error);

    protected static FlowChatResult<AggregateMutation<TResponse>> Mutation(
        MutationType mutationType,
        TResponse response) =>
        FlowChatResult<AggregateMutation<TResponse>>.Success(new AggregateMutation<TResponse>(response, mutationType));

    protected abstract Task<FlowChatResult<AggregateMutation<TResponse>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected virtual TAggregate GetAggregateRoot() =>
        AggregateRoot ?? throw new InvalidOperationException("Aggregate root instance is not available.");

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

    protected Task DispatchLocalEventsAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _localEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
