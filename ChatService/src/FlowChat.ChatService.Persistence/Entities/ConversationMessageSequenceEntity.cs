using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationMessageSequenceEntity
{
    public Id<Conversation> ConversationId { get; init; } = null!;
    public long LastAssignedSequenceNum { get; private set; }
}
