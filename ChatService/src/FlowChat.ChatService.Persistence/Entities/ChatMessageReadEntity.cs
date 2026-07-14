using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ChatMessageReadEntity : ReadEntityBase
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; init; }
}
