namespace FlowChat.Core.Messaging;

public sealed record DeltaProjectionIntegrationEventV2<TValue> : IntegrationEvent
    where TValue : notnull
{
    public required BatchOperationType BatchOperationType { get; init; }
    public required IReadOnlyList<DeltaProjectionItemV2<TValue>> Delta { get; init; }
}
