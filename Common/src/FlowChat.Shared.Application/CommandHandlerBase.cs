using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class CommandHandlerBase<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
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

    public async Task<FlowChatResult<TResponse>> Handle(TCommand request, CancellationToken cancellationToken)
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
        catch (DbUpdateException exception)
        {
            return await HandleDbUpdateExceptionAsync(request, exception, cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected abstract IAggregateRoot? GetAggregateRoot(FlowChatResult<TResponse> result);

    protected virtual Task<FlowChatResult<TResponse>> HandleDbUpdateExceptionAsync(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        return HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
    }

    protected virtual Task<FlowChatResult<TResponse>> HandleUnexpectedExceptionAsync(
        TCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }

    protected Task DispatchDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }

    private sealed class CommandFailedException(FlowChatResult<TResponse> result) : Exception
    {
        public FlowChatResult<TResponse> Result { get; } = result;
    }
}
