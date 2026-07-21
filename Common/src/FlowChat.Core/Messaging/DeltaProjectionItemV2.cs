namespace FlowChat.Core.Messaging;

public sealed record DeltaProjectionItemV2<TValue>
    where TValue : notnull
{
    public required Guid SourceAggregateId { get; init; }
    public required DateTimeOffset SourceAggregateCreatedAtUtc { get; init; }
    public required DateTimeOffset SourceAggregateModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceAggregateDeletedAt { get; init; }
    public required int SourceAggregateVersion { get; init; }
    public required TValue Value { get; init; }
    public required OperationType Operation { get; init; }
}
