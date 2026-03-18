namespace FlowChat.RealtimeService.Domain.Notifications;

public sealed record PresenceChangedNotification(
    Guid UserId,
    string Status,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
