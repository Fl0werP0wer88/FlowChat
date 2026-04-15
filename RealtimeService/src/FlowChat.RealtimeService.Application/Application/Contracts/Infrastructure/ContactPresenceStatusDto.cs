using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record ContactPresenceStatusDto(Guid UserId, PresenceStatus Status, DateTimeOffset ChangedAtUtc);
