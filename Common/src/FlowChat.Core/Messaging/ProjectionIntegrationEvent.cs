namespace FlowChat.Core.Messaging;

public sealed record ProjectionIntegrationEvent<TValue> : IntegrationEvent
{
    public required Guid SourceAggregateId { get; init; }
    public required TValue Value { get; init; }
    public OperationType Operation { get; init; }
    public int Version { get; init; }
}
