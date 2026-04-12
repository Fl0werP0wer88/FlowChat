using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Application.Features.Presence;

public sealed record PresenceStatusSnapshot(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc);
