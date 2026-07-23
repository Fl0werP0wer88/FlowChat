using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
public sealed record AggregateDeltaBatch<TAggregate>(
    IReadOnlyList<AggregateDeltaMutation<TAggregate>> Mutations,
    DeltaProjectionMetadataV2? DeltaProjectionMetadata = null)
    where TAggregate : class, IAggregateRoot;
