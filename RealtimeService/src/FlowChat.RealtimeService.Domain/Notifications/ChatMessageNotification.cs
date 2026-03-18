namespace FlowChat.RealtimeService.Domain.Notifications;

public sealed record ChatMessageNotification(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTime SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
