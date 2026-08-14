using FlowChat.Shared.Persistance;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationMessageSequenceReadEntityV2 : ReadEntityBase
{
    public Guid ConversationId { get; init; }
    public long LastAssignedSequenceNum { get; init; }
}
