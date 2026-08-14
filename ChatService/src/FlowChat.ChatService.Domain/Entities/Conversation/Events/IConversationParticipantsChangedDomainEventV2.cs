using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public interface IConversationParticipantsChangedDomainEventV2
{
    Id<ConversationV2> ConversationId { get; }
    int Version { get; }
}
