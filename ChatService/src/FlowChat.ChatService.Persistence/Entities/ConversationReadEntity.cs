namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationReadEntity
{
    public Guid Id { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
}
