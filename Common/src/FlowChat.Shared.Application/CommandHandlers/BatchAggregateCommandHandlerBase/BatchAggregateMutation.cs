using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

public sealed record BatchAggregateMutation<TResponse, TAggregate>(
    TResponse Response,
    IReadOnlyList<AggregateMutationDescriptor<TAggregate>> Mutations)
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot;
