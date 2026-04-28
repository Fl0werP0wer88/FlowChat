using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application;

public abstract class UpsertCommandHandlerBase<TCommand, TValue>
    : CommandHandlerBase<TCommand, UpsertResult<TValue>>
    where TCommand : ICommand<UpsertResult<TValue>>, IRequest<FlowChatResult<UpsertResult<TValue>>>
    where TValue : notnull
{
    protected UpsertCommandHandlerBase(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork)
        : base(domainEventDispatcher, unitOfWork)
    {
    }

    protected sealed override async Task<FlowChatResult<UpsertResult<TValue>>> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var created = await CreateAsync(request, cancellationToken);
        return created.IsSuccess
            ? FlowChatResult<UpsertResult<TValue>>.Success(
                new UpsertResult<TValue>(created.Value, WasCreated: true))
            : FlowChatResult<UpsertResult<TValue>>.Failure(created.Error);
    }

    protected sealed override IAggregateRoot? GetAggregateRoot(FlowChatResult<UpsertResult<TValue>> result)
    {
        return result.IsSuccess && result.Value.WasCreated
            ? GetCreatedAggregateRoot(result.Value)
            : null;
    }

    protected override async Task<FlowChatResult<UpsertResult<TValue>>> HandleUnexpectedExceptionAsync(
        TCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbUpdateException
            || !dbUpdateException.IsUniqueConstraintViolation())
        {
            return await base.HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }

        var existing = await TryGetExistingAsync(request, cancellationToken);
        if (!existing.Found)
        {
            return await base.HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }

        return FlowChatResult<UpsertResult<TValue>>.Success(
            new UpsertResult<TValue>(existing.Value, WasCreated: false));
    }

    protected abstract Task<(bool Found, TValue Value)> TryGetExistingAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected abstract Task<FlowChatResult<TValue>> CreateAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected abstract IAggregateRoot? GetCreatedAggregateRoot(UpsertResult<TValue> result);
}
