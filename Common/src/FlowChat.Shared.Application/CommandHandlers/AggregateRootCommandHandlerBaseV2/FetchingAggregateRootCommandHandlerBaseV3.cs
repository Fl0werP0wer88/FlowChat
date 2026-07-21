using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class FetchingAggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    : AggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    protected FetchingAggregateRootCommandHandlerBaseV3(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
    {
    }

    protected abstract Task<FlowChatResult<TAggregate?>> FetchAggregateRootAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected override async Task<FlowChatResult<TResponse>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        var fetchResult = await FetchAggregateRootAsync(request, cancellationToken);
        if (fetchResult.IsFailure)
        {
            return FlowChatResult<TResponse>.Failure(fetchResult.Error);
        }

        if (fetchResult.Value is not null)
        {
            AggregateRoot = fetchResult.Value;
        }

        return await base.HandleInTransactionAsync(request, cancellationToken);
    }
}
