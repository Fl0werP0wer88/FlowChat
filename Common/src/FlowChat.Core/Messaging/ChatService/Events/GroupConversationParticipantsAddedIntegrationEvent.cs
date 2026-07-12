namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed record GroupConversationParticipantsAddedIntegrationEvent : IntegrationEvent
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
    public int ConversationVersion { get; init; }
}
