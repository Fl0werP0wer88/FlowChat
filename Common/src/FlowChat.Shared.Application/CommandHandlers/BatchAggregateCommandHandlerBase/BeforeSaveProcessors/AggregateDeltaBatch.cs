using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public sealed record AggregateDeltaBatch<TAggregate>(
    BatchOperationType BatchOperationType,
    IReadOnlyList<AggregateDeltaMutation<TAggregate>> Mutations)
    where TAggregate : class, IAggregateRoot;
