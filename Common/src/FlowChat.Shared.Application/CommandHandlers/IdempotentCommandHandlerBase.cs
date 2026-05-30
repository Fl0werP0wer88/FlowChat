using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public abstract class IdempotentCommandHandlerBase<TCommand, TValue>
    : AggregateRootCommandHandlerBase<TCommand, IdempotentCommandResult<TValue>>
    where TCommand : ICommand<IdempotentCommandResult<TValue>>, IRequest<FlowChatResult<IdempotentCommandResult<TValue>>>
    where TValue : notnull
{
    private readonly IDbUpdateExceptionClassifier _dbUpdateExceptionClassifier;

    protected IdempotentCommandHandlerBase(
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork)
    {
        _dbUpdateExceptionClassifier = dbUpdateExceptionClassifier
            ?? throw new ArgumentNullException(nameof(dbUpdateExceptionClassifier));
    }

    protected override async Task<FlowChatResult<IdempotentCommandResult<TValue>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var baseResult = await ExecuteCommandAsync(request, cancellationToken);
        return BuildResponse(baseResult);
    }

    private static FlowChatResult<IdempotentCommandResult<TValue>> BuildResponse(FlowChatResult<TValue> executed)
    {
        return executed.IsSuccess
            ? FlowChatResult<IdempotentCommandResult<TValue>>.Success(
                new IdempotentCommandResult<TValue>(executed.Value, WasAlreadyProcessed: false))
            : FlowChatResult<IdempotentCommandResult<TValue>>.Failure(executed.Error);
    }

    protected override async Task<FlowChatResult<IdempotentCommandResult<TValue>>> OnDbUpdateExceptionAfterRollbackHook(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (!_dbUpdateExceptionClassifier.IsIdempotencyConflict(
                exception,
                GetIdempotencyConflictKey(request)))
        {
            return await base.OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken);
        }

        var existing = await TryGetExistingResponseAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await base.OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken);
        }

        return FlowChatResult<IdempotentCommandResult<TValue>>.Success(
            new IdempotentCommandResult<TValue>(existing.Value, WasAlreadyProcessed: true));
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

}
