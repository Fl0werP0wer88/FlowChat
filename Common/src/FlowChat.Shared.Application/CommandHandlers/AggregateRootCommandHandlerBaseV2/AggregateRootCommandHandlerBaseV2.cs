using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootCommandHandlerBaseV2<TCommand, TResponse, TAggregate>
    : TransactionalCommandHandlerBase<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> _beforeSaveProcessors;

    protected AggregateRootCommandHandlerBaseV2(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> beforeSaveProcessors)
        : base(unitOfWork)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
    }

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var operationResult = await ExecuteAsync(request, cancellationToken);

        if (operationResult.IsSuccess)
        {
            var aggregateRoot = GetAggregateRoot();

            var aggregateState = GetAggregateState(request, aggregateRoot);
            if (aggregateState == AggregateState.Unchanged)
            {
                return operationResult;
            }

            aggregateRoot.IncrementVersion();
            var domainEvents = aggregateRoot.PopDomainEvents();
            var localEvents = domainEvents
                .Cast<ILocalEvent>();

            await DispatchLocalEventsAsync(localEvents, cancellationToken);
            ApplyAuditInfo(aggregateRoot, aggregateState);

            foreach (var processor in _beforeSaveProcessors)
            {
                await processor.ProcessAsync(request, aggregateRoot, aggregateState, cancellationToken);
            }
        }

        return operationResult;
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract TAggregate GetAggregateRoot();

    protected abstract AggregateState GetAggregateState(TCommand request, TAggregate aggregateRoot);

    private static void ApplyAuditInfo(TAggregate aggregateRoot, AggregateState aggregateState)
    {
        const string SystemActor = "system";

        switch (aggregateState)
        {
            case AggregateState.Created:
                aggregateRoot.SetCreated(SystemActor);
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case AggregateState.Updated:
                aggregateRoot.SetUpdated(SystemActor);
                break;
            case AggregateState.Deleted:
                aggregateRoot.SetUpdated(SystemActor);
                aggregateRoot.Delete(UtcDateTimeOffset.UtcNow);
                break;
            case AggregateState.Unchanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(aggregateState), aggregateState, null);
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
