using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class AggregateRootUpdateDomainEventHandlerBase<TNotification, TAggregate>
    : AggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootUpdateDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TNotification, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
    {
    }

    protected void SetUpdated() => SetMutationType(MutationType.Updated);
}
