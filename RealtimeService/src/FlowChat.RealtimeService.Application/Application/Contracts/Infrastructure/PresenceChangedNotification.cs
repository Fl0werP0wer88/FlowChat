using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record PresenceChangedNotification(
    Guid UserId,
    UserPresenceStatus Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
