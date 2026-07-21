using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class FetchingAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    : AggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected FetchingAggregateRootDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors)
    {
    }

    protected abstract Task<FlowChatResult<TAggregate?>> FetchAggregateRootAsync(
        TNotification notification,
        CancellationToken cancellationToken);

    protected override async Task HandleNotificationAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var fetchResult = await FetchAggregateRootAsync(notification, cancellationToken);
        if (fetchResult.IsFailure)
        {
            throw new ResultException(FlowChatResult.Failure(fetchResult.Error));
        }

        if (fetchResult.Value is not null)
        {
            AggregateRoot = fetchResult.Value;
        }

        await base.HandleNotificationAsync(notification, cancellationToken);
    }
}
