using FlowChat.Core.Messaging;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

public sealed record BatchAggregateMutation<TResponse, TAggregate>(
    TResponse Response,
    BatchOperationType BatchOperationType,
    IReadOnlyList<AggregateMutationDescriptor<TAggregate>> Mutations,
    DeltaProjectionMetadataV2? DeltaProjectionMetadata = null)
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot;
