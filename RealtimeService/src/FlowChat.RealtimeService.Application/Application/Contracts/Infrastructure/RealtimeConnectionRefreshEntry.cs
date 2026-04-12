using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record RealtimeConnectionRefreshEntry(Guid UserId, string ConnectionId, UserStatus Status);
