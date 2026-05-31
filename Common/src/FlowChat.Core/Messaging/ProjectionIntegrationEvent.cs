namespace FlowChat.Core.Messaging;

public sealed record ProjectionIntegrationEvent<TValue> : IntegrationEvent
{
    public required TValue Value { get; init; }
    public OperationTypes Operation { get; init; }
}
