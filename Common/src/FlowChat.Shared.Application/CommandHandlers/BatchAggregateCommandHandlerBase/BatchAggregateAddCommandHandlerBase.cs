using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

public abstract class BatchAggregateAddCommandHandlerBase<TCommand, TResponse, TAggregate>
    : BatchAggregateCommandHandlerBase<TCommand, TResponse, TAggregate>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    protected BatchAggregateAddCommandHandlerBase(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> beforeSaveProcessors,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TCommand, TAggregate>> beforeSaveDeltaProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors, beforeSaveDeltaProcessors)
    {
    }

    //Review3 : Nie podoba mi sie nazwa tej metody może tak lepiej po prostu GetResult() Oceń pomysł.
    protected static FlowChatResult<BatchAggregateMutation<TResponse, TAggregate>> AddBatch(
        TResponse response,
        IReadOnlyList<Id<TAggregate>> aggregateIds,
        DeltaProjectionMetadataV2 deltaProjectionMetadata)
        => Mutation(
            response,
            aggregateIds,
            MutationType.Created,
            BatchOperationType.Created,
            deltaProjectionMetadata);
}
