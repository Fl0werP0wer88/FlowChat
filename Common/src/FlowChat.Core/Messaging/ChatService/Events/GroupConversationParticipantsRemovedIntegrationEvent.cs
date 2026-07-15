namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed record GroupConversationParticipantsRemovedIntegrationEvent : IntegrationEvent
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
    public int ConversationMembershipRevision { get; init; }
}
