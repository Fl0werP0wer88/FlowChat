namespace FlowChat.Core.Messaging;

public sealed record DeltaProjectionIntegrationEventV2<TValue> : IntegrationEvent
    where TValue : notnull
{
    public required IReadOnlyList<DeltaProjectionItemV2<TValue>> Delta { get; init; }
}
