using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record PresenceChangedParam(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
