using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.ReadModels;

public sealed class DuetConversation
{
    public Guid FirstUserId { get; set; }
    public Guid SecondUserId { get; set; }
    public Id<Conversation> ConversationId { get; set; } = Id<Conversation>.New();
}
