namespace FlowChat.ChatService.Persistence.Entities;

public sealed class DuetConversationReadEntity
{
    public Guid FirstUserId { get; init; }
    public Guid SecondUserId { get; init; }
    public Guid ConversationId { get; init; }
}
