namespace FlowChat.ChatService.Persistence.ReadModels;

public sealed class DuetConversation
{
    public Guid FirstUserId { get; set; }
    public Guid SecondUserId { get; set; }
    public Guid ConversationId { get; set; }
}
