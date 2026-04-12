using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record PresenceChangedNotification(
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
