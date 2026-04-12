using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Application.Features.Presence;

public sealed record PresenceStatusSnapshot(
    Guid UserId,
    UserPresenceStatus Status,
    DateTimeOffset ChangedAtUtc);
