using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;

public sealed record DeltaProjectionMetadataV2(
    Guid ProjectionId,
    int ProjectionRevision,
    BatchOperationType ProjectionOperationType);
