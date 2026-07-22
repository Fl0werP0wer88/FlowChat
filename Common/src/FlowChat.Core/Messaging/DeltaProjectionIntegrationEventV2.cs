namespace FlowChat.Core.Messaging;

public sealed record DeltaProjectionIntegrationEventV2<TValue> : IntegrationEvent
    where TValue : notnull
{
    public required Guid ProjectionId { get; init; }
    public required int ProjectionRevision { get; init; }
    public required BatchOperationType ProjectionOperationType { get; init; }
    public required IReadOnlyList<DeltaProjectionItemV2<TValue>> Delta { get; init; }
}
