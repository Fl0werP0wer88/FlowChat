using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public abstract class IdempotentCommandHandlerBase<TCommand, TValue>
    : CommandHandlerBase<TCommand, IdempotentCommandResult<TValue>>
    where TCommand : ICommand<IdempotentCommandResult<TValue>>, IRequest<FlowChatResult<IdempotentCommandResult<TValue>>>
    where TValue : notnull
{
    private readonly IDbUpdateExceptionClassifier _dbUpdateExceptionClassifier;

    protected IdempotentCommandHandlerBase(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork)
    {
        _dbUpdateExceptionClassifier = dbUpdateExceptionClassifier
            ?? throw new ArgumentNullException(nameof(dbUpdateExceptionClassifier));
    }

    protected sealed override async Task<FlowChatResult<IdempotentCommandResult<TValue>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var executed = await ExecuteCommandAsync(request, cancellationToken);
        return executed.IsSuccess
            ? FlowChatResult<IdempotentCommandResult<TValue>>.Success(
                new IdempotentCommandResult<TValue>(executed.Value, WasAlreadyProcessed: false))
            : FlowChatResult<IdempotentCommandResult<TValue>>.Failure(executed.Error);
    }

    protected sealed override IAggregateRoot? GetAggregateRoot(FlowChatResult<IdempotentCommandResult<TValue>> result)
    {
        return result.IsSuccess && !result.Value.WasAlreadyProcessed
            ? GetExecutedAggregateRoot(result.Value)
            : null;
    }

    protected override async Task<FlowChatResult<IdempotentCommandResult<TValue>>> HandleDbUpdateExceptionAsync(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (!_dbUpdateExceptionClassifier.IsExpectedIdempotencyConflict(
                exception,
                GetIdempotencyConflictKey(request)))
        {
            return await base.HandleDbUpdateExceptionAsync(request, exception, cancellationToken);
        }

        var existing = await TryGetExistingResponseAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await base.HandleDbUpdateExceptionAsync(request, exception, cancellationToken);
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

    protected abstract IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<TValue> result);

    protected virtual string GetIdempotencyConflictKey(TCommand request)
    {
        return typeof(TCommand).FullName ?? typeof(TCommand).Name;
    }
}
