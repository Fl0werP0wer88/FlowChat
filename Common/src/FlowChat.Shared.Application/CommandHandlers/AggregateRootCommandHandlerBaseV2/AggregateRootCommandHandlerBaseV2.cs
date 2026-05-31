using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
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

    protected OperationTypes ProjectionOperationType { get; }

    protected AggregateRootCommandHandlerBaseV2(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> beforeSaveProcessors,
        OperationTypes projectionOperationType)
        : base(unitOfWork)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _beforeSaveProcessors = beforeSaveProcessors;
        ProjectionOperationType = projectionOperationType;
    }

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var operationResult = await ExecuteAsync(request, cancellationToken);

        if (operationResult.IsSuccess)
        {
            var aggregateRoot = GetAggregateRoot();

            aggregateRoot.IncrementVersion();
            // var snapshot = _mapper.Map<TSnapshot>(aggregateRoot);
            // var snapshotEvent = new SnapshotApplicationEvent<TSnapshot>(snapshot);
            var domainEvents = aggregateRoot.PopDomainEvents();
            var localEvents = domainEvents
                .Cast<ILocalEvent>();
            // .Append(snapshotEvent);

            await DispatchLocalEventsAsync(localEvents, cancellationToken);

            foreach (var processor in _beforeSaveProcessors)
            {
                await processor.ProcessAsync(request, aggregateRoot, ProjectionOperationType, cancellationToken);
            }
        }

        return operationResult;
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract TAggregate GetAggregateRoot();

    protected Task DispatchLocalEventsAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _localEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
