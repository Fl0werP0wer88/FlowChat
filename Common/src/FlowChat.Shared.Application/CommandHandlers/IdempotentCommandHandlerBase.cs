using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class IdempotentCommandHandlerBase<TCommand, TValue>
    : ICommandHandler<TCommand, IdempotentCommandResult<TValue>>
    where TCommand : ICommand<IdempotentCommandResult<TValue>>, IRequest<FlowChatResult<IdempotentCommandResult<TValue>>>
    where TValue : notnull
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbUpdateExceptionClassifier _dbUpdateExceptionClassifier;

    protected IdempotentCommandHandlerBase(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _unitOfWork = unitOfWork;
        _dbUpdateExceptionClassifier = dbUpdateExceptionClassifier
            ?? throw new ArgumentNullException(nameof(dbUpdateExceptionClassifier));
    }

    public async Task<FlowChatResult<IdempotentCommandResult<TValue>>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var operationResult = await ExecuteAsync(request, token);
                    if (!operationResult.IsSuccess)
                    {
                        throw new CommandFailedException(operationResult);
                    }

                    if (IsCommandFirstSucessfullRun(operationResult))
                    {
                        var aggregateRoot = GetAggregateRoot();
                        if (aggregateRoot is not null)
                        {
                            var domainEvents = aggregateRoot.PopDomainEvents();
                            await DispatchDomainEventsAsync(domainEvents, token);
                        }
                    }


                    return operationResult;
                },
                cancellationToken);
        }
        catch (CommandFailedException exception)
        {
            return exception.Result;
        }
        catch (DbUpdateException exception)
        {
            // Keep this outside the unit of work so EF execution strategies can finish all retries before application-specific recovery runs
            return await OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }

    private async Task<FlowChatResult<IdempotentCommandResult<TValue>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var executed = await ExecuteCommandAsync(request, cancellationToken);
        return executed.IsSuccess
            ? FlowChatResult<IdempotentCommandResult<TValue>>.Success(
                new IdempotentCommandResult<TValue>(executed.Value, WasAlreadyProcessed: false))
            : FlowChatResult<IdempotentCommandResult<TValue>>.Failure(executed.Error);
    }

    private bool IsCommandFirstSucessfullRun(FlowChatResult<IdempotentCommandResult<TValue>> result)
    {
        return result.IsSuccess && !result.Value.WasAlreadyProcessed
            // ? GetAggregateRoot()
            // : null;
    }

    protected abstract IAggregateRoot? GetAggregateRoot();

    protected virtual async Task<FlowChatResult<IdempotentCommandResult<TValue>>> OnDbUpdateExceptionAfterRollbackHook(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (!_dbUpdateExceptionClassifier.IsExpectedIdempotencyConflict(
                exception,
                GetIdempotencyConflictKey(request)))
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }

        var existing = await TryGetExistingResponseAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }

        return FlowChatResult<IdempotentCommandResult<TValue>>.Success(
            new IdempotentCommandResult<TValue>(existing.Value, WasAlreadyProcessed: true));
    }

    protected virtual Task<FlowChatResult<IdempotentCommandResult<TValue>>> HandleUnexpectedExceptionAsync(
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

    protected abstract Task<(bool Found, TValue Value)> TryGetExistingResponseAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected abstract Task<FlowChatResult<TValue>> ExecuteCommandAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected virtual string GetIdempotencyConflictKey(TCommand request)
    {
        return typeof(TCommand).FullName ?? typeof(TCommand).Name;
    }

    private sealed class CommandFailedException(FlowChatResult<IdempotentCommandResult<TValue>> result) : Exception
    {
        public FlowChatResult<IdempotentCommandResult<TValue>> Result { get; } = result;
    }
}
