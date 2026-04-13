namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record RealtimeConnectionMutationResult(
    Guid UserId,
    string ConnectionId,
    int ActiveConnectionCount,
    DateTimeOffset OccurredAtUtc);
