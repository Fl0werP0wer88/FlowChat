using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class DuetConversationReadEntityV2 : ReadEntityBase
{
    public Guid FirstUserId { get; init; }
    public Guid SecondUserId { get; init; }
    public Guid ConversationId { get; init; }
}
