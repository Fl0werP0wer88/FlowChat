namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record PresenceChangedNotification(
    Guid UserId,
    string Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
