using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class AggregateRootCommandHandlerBaseV2<TCommand, TResponse, TAggregate> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    private readonly ILocalEventDispatcher _localEventsDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    IEnumerable<IAggregatePostProcessor<TCommand, TAggregate>> _postProcessors;

    protected AggregateRootCommandHandlerBaseV2(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregatePostProcessor<TCommand, TAggregate>> postProcessors)
    {
        _localEventsDispatcher = localEventsDispatcher;
        _unitOfWork = unitOfWork;
        _postProcessors = postProcessors;
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
                            // var snapshot = _mapper.Map<TSnapshot>(aggregateRoot);
                            // var snapshotEvent = new SnapshotApplicationEvent<TSnapshot>(snapshot);
                            var domainEvents = aggregateRoot.PopDomainEvents();
                            var localEvents = domainEvents
                                .Cast<ILocalEvent>();
                            // .Append(snapshotEvent);

                            await DispatchLocalEventsAsync(localEvents, token);

                            foreach (var processor in _postProcessors)
                            {
                                await processor.ProcessAsync(request, aggregateRoot, cancellationToken);
                            }
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

    protected virtual TAggregate? GetAggregateRoot() => null;

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

        return _localEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}


public interface IAggregatePostProcessor<TCommand, TAggregate>
{
    Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        CancellationToken cancellationToken);
}