namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public sealed record ChatMessageParam(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    long SequenceNum,
    DateTimeOffset SentAtUtc,
    DateTimeOffset DeliveredAtUtc,
    IReadOnlyCollection<Guid> RecipientUserIds);
