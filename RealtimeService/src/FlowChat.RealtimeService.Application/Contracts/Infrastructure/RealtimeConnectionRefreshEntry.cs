namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record RealtimeConnectionRefreshEntry(Guid UserId, string ConnectionId);
