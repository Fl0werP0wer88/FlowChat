namespace FlowChat.Core.Messaging.RealtimeService.Events;

public sealed record RealtimeConnectionUnregisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public required string ConnectionId { get; init; }
    public int ActiveConnectionCount { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
}
