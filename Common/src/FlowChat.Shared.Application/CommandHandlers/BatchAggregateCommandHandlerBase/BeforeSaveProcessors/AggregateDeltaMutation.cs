using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public sealed record AggregateDeltaMutation<TAggregate>(
    TAggregate Aggregate,
    MutationType MutationType)
    where TAggregate : class, IAggregateRoot;
