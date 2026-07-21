using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public abstract class BatchAggregateRootRemoveDomainEventHandlerBase<TNotification, TAggregate>
    : BatchAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected BatchAggregateRootRemoveDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TNotification, TAggregate>> beforeSaveDeltaProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors, beforeSaveDeltaProcessors)
    {
    }

    protected static FlowChatResult<IReadOnlyList<AggregateMutationDescriptor<TAggregate>>> RemoveBatch(
        IReadOnlyList<Id<TAggregate>> aggregateIds)
        => Mutations(aggregateIds, MutationType.Deleted);
}
