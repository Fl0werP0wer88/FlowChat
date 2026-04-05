namespace FlowChat.RealtimeService.Domain.Notifications;

public sealed record PresenceChangedNotification(
    Guid UserId,
    string Status,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
