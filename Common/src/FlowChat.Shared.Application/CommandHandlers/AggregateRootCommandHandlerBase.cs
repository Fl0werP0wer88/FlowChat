using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class AggregateRootCommandHandlerBase<TCommand, TResponse>
    : TransactionalCommandHandlerBase<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;

    protected AggregateRootCommandHandlerBase(
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        _localEventsDispatcher = domainEventDispatcher;
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
            var domainEvents = aggregateRoot.PopDomainEvents();
            await DispatchLocalEventsAsync(domainEvents, cancellationToken);
        }

        return operationResult;
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract IAggregateRoot GetAggregateRoot();

    protected Task DispatchLocalEventsAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _localEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }

}
