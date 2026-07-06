namespace FlowChat.RealtimeService.Api.Realtime.Notifications;

public sealed class ChatMessageReceivedNotification
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public required string SenderDisplayName { get; init; }
    public required string Text { get; init; }
    public long SequenceNum { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public DateTimeOffset DeliveredAtUtc { get; init; }
}
