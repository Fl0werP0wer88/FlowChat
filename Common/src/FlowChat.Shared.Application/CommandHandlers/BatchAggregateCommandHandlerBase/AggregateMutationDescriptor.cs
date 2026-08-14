using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

public sealed record AggregateMutationDescriptor<TAggregate>(
    Id<TAggregate> Id,
    MutationType MutationType)
    where TAggregate : class, IAggregateRoot;
