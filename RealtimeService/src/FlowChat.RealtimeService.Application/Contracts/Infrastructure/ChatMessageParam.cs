namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record ChatMessageParam(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
