using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public sealed record BatchAggregateDomainEventMutation<TAggregate>(
    BatchOperationType BatchOperationType,
    IReadOnlyList<AggregateMutationDescriptor<TAggregate>> Mutations)
    where TAggregate : class, IAggregateRoot;
