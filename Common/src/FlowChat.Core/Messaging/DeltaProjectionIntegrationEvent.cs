namespace FlowChat.Core.Messaging;

public sealed record DeltaProjectionIntegrationEvent<TValue> : IntegrationEvent
{
    public required Guid SourceAggregateId { get; init; }
    public required DateTimeOffset SourceAggregateCreatedAtUtc { get; init; }
    public required DateTimeOffset SourceAggregateModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceAggregateDeletedAt { get; init; }
    public required IEnumerable<TValue> Value { get; init; }
    public required DeltaOperationType Operation { get; init; }
    public int SourceAggregateVersion { get; init; }
}
