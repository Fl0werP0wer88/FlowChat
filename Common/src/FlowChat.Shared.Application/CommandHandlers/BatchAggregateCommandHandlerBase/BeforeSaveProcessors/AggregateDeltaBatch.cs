using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
//Review3: Nie lepiej żeby BatchOperationType to trafilo do DeltaProjectionMetadataV2 jako pole ProjectionOperationType ?
public sealed record AggregateDeltaBatch<TAggregate>(
    BatchOperationType BatchOperationType,
    IReadOnlyList<AggregateDeltaMutation<TAggregate>> Mutations,
    DeltaProjectionMetadataV2? DeltaProjectionMetadata = null)
    where TAggregate : class, IAggregateRoot;
