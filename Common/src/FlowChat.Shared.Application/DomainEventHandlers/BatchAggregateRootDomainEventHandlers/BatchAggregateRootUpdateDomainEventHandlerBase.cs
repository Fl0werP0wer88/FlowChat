using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public abstract class BatchAggregateRootUpdateDomainEventHandlerBase<TNotification, TAggregate>
    : BatchAggregateRootDomainEventHandlerBase<TNotification, TAggregate>
    where TNotification : IDomainEvent
    where TAggregate : class, IAggregateRoot
{
    protected BatchAggregateRootUpdateDomainEventHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TNotification, TAggregate>> beforeSaveProcessors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TNotification, TAggregate>> beforeSaveDeltaProcessors)
        : base(localEventsDispatcher, beforeSaveProcessors, beforeSaveDeltaProcessors)
    {
    }

    protected static FlowChatResult<BatchAggregateDomainEventMutation<TAggregate>> UpdateBatch(
        IReadOnlyList<Id<TAggregate>> aggregateIds)
        => Mutations(aggregateIds, MutationType.Updated, BatchOperationType.Updated);
}
