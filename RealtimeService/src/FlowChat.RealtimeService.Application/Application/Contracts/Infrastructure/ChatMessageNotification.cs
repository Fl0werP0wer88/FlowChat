namespace FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;

public sealed record ChatMessageNotification(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
