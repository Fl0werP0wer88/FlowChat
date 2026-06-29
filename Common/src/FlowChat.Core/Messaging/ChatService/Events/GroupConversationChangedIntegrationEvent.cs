namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed record GroupConversationChangedIntegrationEvent : IntegrationEvent
{
    public Guid ConversationId { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
    public Guid CreatedByUserId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}
