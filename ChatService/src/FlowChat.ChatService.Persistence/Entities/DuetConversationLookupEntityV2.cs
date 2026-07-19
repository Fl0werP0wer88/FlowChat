using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.Entities;

public sealed class DuetConversationLookupEntityV2
{
    public Id<ConversationV2> ConversationId { get; init; } = null!;
    public Guid FirstUserId { get; init; }
    public Guid SecondUserId { get; init; }
    public DateTimeOffset? DeletedAt { get; private set; }
}
