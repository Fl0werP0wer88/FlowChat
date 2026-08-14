using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class ConversationMessageSequenceEntityV2
{
    public Id<ConversationV2> ConversationId { get; init; } = null!;
    public long LastAssignedSequenceNum { get; private set; }
}
