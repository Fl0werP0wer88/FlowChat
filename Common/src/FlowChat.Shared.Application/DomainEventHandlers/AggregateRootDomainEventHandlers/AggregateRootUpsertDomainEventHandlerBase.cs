using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;

public abstract class AggregateRootUpsertDomainEventHandlerBase<TNotification, TAggregate>
    : FetchingAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootUpsertDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<TNotification, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors)
    {
    }

    protected static FlowChatResult<MutationType> Created() =>
        Mutation(MutationType.Created);

    protected static FlowChatResult<MutationType> Updated() =>
        Mutation(MutationType.Updated);
}
