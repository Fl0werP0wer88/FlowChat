using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class AggregateRootDeleteDomainEventHandlerBase<TNotification, TAggregate>
    : FetchingAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootDeleteDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<TNotification, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors)
    {
    }

    protected static FlowChatResult<MutationType> Deleted() =>
        Mutation(MutationType.Deleted);
}
