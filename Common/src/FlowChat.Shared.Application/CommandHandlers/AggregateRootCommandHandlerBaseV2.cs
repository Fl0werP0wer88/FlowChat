using AutoMapper;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class AggregateRootCommandHandlerBaseV2<TCommand, TResponse, TSnapshot> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    protected AggregateRootCommandHandlerBaseV2(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
                            var snapshot = _mapper.Map<TSnapshot>(aggregateRoot);
                            aggregateRoot.IncrementVersion();
                            var domainEvents = aggregateRoot.PopDomainEvents();
                            await DispatchDomainEventsAsync(domainEvents, token);
                        }
                    }

                    return operationResult;
                },
                cancellationToken);
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

    protected abstract Task<FlowChatResult<TResponse>> ExecuteAsync(TCommand request, CancellationToken cancellationToken);

    protected virtual IAggregateRoot? GetAggregateRoot() => null;

    protected virtual Task<FlowChatResult<TResponse>> OnDbUpdateExceptionAfterRollbackHook(
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
}
