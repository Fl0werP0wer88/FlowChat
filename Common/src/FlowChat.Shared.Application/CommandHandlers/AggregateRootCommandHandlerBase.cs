using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class AggregateRootCommandHandlerBase<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
    private readonly ILocalEventDispatcher _localEvenstDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    protected AggregateRootCommandHandlerBase(
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork)
    {
        _localEvenstDispatcher = domainEventDispatcher;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowChatResult<TResponse>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteCommandInTransactionAsync(
                async token =>
                {
                    var operationResult = await ExecuteAsync(request, token);

                    if (operationResult.IsSuccess)
                    {
                        var aggregateRoot = GetAggregateRoot();
                        if (aggregateRoot is not null)
                        {
                            aggregateRoot.IncrementVersion();
                            var domainEvents = aggregateRoot.PopDomainEvents();
                            await DispatchLocalEventsAsync(domainEvents, token);
                        }
                    }

                    return operationResult;
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected virtual IAggregateRoot? GetAggregateRoot() => null;

    protected virtual Task<FlowChatResult<TResponse>> HandleUnexpectedExceptionAsync(
        TCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }

    protected Task DispatchLocalEventsAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents is null)
        {
            return Task.CompletedTask;
        }

        return _localEvenstDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }

}
