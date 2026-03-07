using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public abstract class CommandHandlerBase<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<Result<TResponse, IDomainError>>
    where TResponse : notnull
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    protected CommandHandlerBase(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TResponse, IDomainError>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var operationResult = await ExecuteAsync(request, token);
                if (!operationResult.IsSuccess)
                {
                    throw new CommandFailedException(operationResult);
                }

                var aggregateRoot = GetAggregateRoot(operationResult);
                if (aggregateRoot is not null)
                {
                    var domainEvents = aggregateRoot.PopDomainEvents();
                    await DispatchDomainEventsAsync(domainEvents, token);
                }

                return operationResult;
            }, cancellationToken);
        }
        catch (CommandFailedException exception)
        {
            return exception.Result;
        }
    }

    protected abstract Task<Result<TResponse, IDomainError>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract IAggregateRoot? GetAggregateRoot(Result<TResponse, IDomainError> result);

    protected Task DispatchDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }

    private sealed class CommandFailedException(Result<TResponse, IDomainError> result) : Exception
    {
        public Result<TResponse, IDomainError> Result { get; } = result;
    }
}
