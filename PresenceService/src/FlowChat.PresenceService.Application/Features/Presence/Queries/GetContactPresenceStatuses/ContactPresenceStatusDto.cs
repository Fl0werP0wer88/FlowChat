using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;

public sealed record ContactPresenceStatusDto(Guid UserId, PresenceStatus Status, DateTimeOffset ChangedAtUtc);
