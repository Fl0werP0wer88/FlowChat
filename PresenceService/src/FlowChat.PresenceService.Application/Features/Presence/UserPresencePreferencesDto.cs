using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.Application.Features.Presence;

public sealed record UserPresencePreferencesDto(
    Guid UserId,
    PresenceStatus PreferredStatus,
    DateTimeOffset LastModifiedAtUtc) : IDbResponse;
