using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public abstract class IdempotentCreateCommandHandlerBase<TCommand, TValue>
    : CommandHandlerBase<TCommand, IdempotentCreateResult<TValue>>
    where TCommand : ICommand<IdempotentCreateResult<TValue>>, IRequest<FlowChatResult<IdempotentCreateResult<TValue>>>
    where TValue : notnull
{
    private readonly IDbUpdateExceptionClassifier _dbUpdateExceptionClassifier;

    protected IdempotentCreateCommandHandlerBase(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork)
    {
        _dbUpdateExceptionClassifier = dbUpdateExceptionClassifier
            ?? throw new ArgumentNullException(nameof(dbUpdateExceptionClassifier));
    }

    protected sealed override async Task<FlowChatResult<IdempotentCreateResult<TValue>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var created = await CreateAsync(request, cancellationToken);
        return created.IsSuccess
            ? FlowChatResult<IdempotentCreateResult<TValue>>.Success(
                new IdempotentCreateResult<TValue>(created.Value, WasCreated: true))
            : FlowChatResult<IdempotentCreateResult<TValue>>.Failure(created.Error);
    }

    protected sealed override IAggregateRoot? GetAggregateRoot(FlowChatResult<IdempotentCreateResult<TValue>> result)
    {
        return result.IsSuccess && result.Value.WasCreated
            ? GetCreatedAggregateRoot(result.Value)
            : null;
    }

    protected override async Task<FlowChatResult<IdempotentCreateResult<TValue>>> HandleDbUpdateExceptionAsync(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (!_dbUpdateExceptionClassifier.IsExpectedUniqueConstraintViolation(
                exception,
                GetExpectedUniqueConstraintNames(request)))
        {
            return await base.HandleDbUpdateExceptionAsync(request, exception, cancellationToken);
        }

        var existing = await TryGetExistingAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await base.HandleDbUpdateExceptionAsync(request, exception, cancellationToken);
        }

        return FlowChatResult<IdempotentCreateResult<TValue>>.Success(
            new IdempotentCreateResult<TValue>(existing.Value, WasCreated: false));
    }

    protected abstract Task<(bool Found, TValue Value)> TryGetExistingAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected abstract Task<FlowChatResult<TValue>> CreateAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected abstract IAggregateRoot? GetCreatedAggregateRoot(IdempotentCreateResult<TValue> result);

    protected virtual IReadOnlyCollection<string> GetExpectedUniqueConstraintNames(TCommand request) => [];
}
