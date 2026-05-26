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
                    var baseResult = await ExecuteCommandAsync(request, cancellationToken);
                    var idempotentResult = await BuildResponse(baseResult);
                    if (!idempotentResult.IsSuccess)
                    {
                        //Im throwing exception here to trigger transaction rollback and catch it outside of the unit of work. This allows any retries from EF execution strategy to happen before we attempt to recover from the failure.
                        throw new CommandFailedException(idempotentResult);
                    }

                    await DispatchDomainEventsAsync(token);

                    return idempotentResult;
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
            return await HandleUnexpectedExceptionAsync(exception);
        }
    }

    private async Task<FlowChatResult<IdempotentCommandResult<TValue>>> BuildResponse(FlowChatResult<TValue> executed)
    {
        return executed.IsSuccess
            ? FlowChatResult<IdempotentCommandResult<TValue>>.Success(
                new IdempotentCommandResult<TValue>(executed.Value, WasAlreadyProcessed: false))
            : FlowChatResult<IdempotentCommandResult<TValue>>.Failure(executed.Error);
    }

    protected virtual IAggregateRoot? GetAggregateRoot()
    {
        return null;
    }

    protected virtual async Task<FlowChatResult<IdempotentCommandResult<TValue>>> OnDbUpdateExceptionAfterRollbackHook(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (!_dbUpdateExceptionClassifier.IsIdempotencyConflict(
                exception,
                GetIdempotencyConflictKey(request)))
        {
            return await HandleUnexpectedExceptionAsync(exception);
        }

        var existing = await TryGetExistingResponseAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await HandleUnexpectedExceptionAsync(exception);
        }

        return FlowChatResult<IdempotentCommandResult<TValue>>.Success(
            new IdempotentCommandResult<TValue>(existing.Value, WasAlreadyProcessed: true));
    }

    private static Task<FlowChatResult<IdempotentCommandResult<TValue>>> HandleUnexpectedExceptionAsync(
        Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }

    protected Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregateRoot = GetAggregateRoot();

        if (aggregateRoot is null)
        {
            return Task.CompletedTask;
        }

        var domainEvents = aggregateRoot.PopDomainEvents();

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
