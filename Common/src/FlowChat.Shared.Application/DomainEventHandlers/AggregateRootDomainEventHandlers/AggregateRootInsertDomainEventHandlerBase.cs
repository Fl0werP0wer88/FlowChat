using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class AggregateRootInsertDomainEventHandlerBase<TNotification, TAggregate>
    : AggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootInsertDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors)
    {
    }

    protected static FlowChatResult<MutationType> Created() =>
        Mutation(MutationType.Created);
}
